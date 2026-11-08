using System.Collections.Immutable;
using EventSourcing.Core;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Commands;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;
using SkillsModule.Application.Commands;
using SkillsModule.Domain;
using SkillsModule.Domain.Events;
using SkillsModule.Domain.Models;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application.Tests;

public sealed partial class ProjectCommandTests
{
    [Fact]
    public async Task ProjectSkillLink_IsWrittenAndRemovedOnBothStreams()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Linked project");
        var skillId = await CreateSkill(handler, "linked-skill");

        await ExecuteAsUser(new AddSkillToProjectCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = skillId.Value
        });

        Assert.Equal([skillId], ProjectState(eventStore, projectId).RelatedSkillIds);
        Assert.Equal([projectId], SkillState(eventStore, skillId).RelatedProjectIds);
        Assert.Equal(
            skillId,
            Assert.IsType<ProjectSkillRelationAddedV1>(
                eventStore.GetStoredEvents(projectId).Last().EventData
            ).SkillId
        );

        await ExecuteAsUser(new RemoveSkillFromProjectCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = skillId.Value
        });

        Assert.Empty(ProjectState(eventStore, projectId).RelatedSkillIds);
        Assert.Empty(SkillState(eventStore, skillId).RelatedProjectIds);
        Assert.IsType<SkillProjectRelationRemovedV1>(
            eventStore.GetStoredEvents(skillId).Last().EventData
        );
    }

    [Theory]
    [InlineData(true, "already linked")]
    [InlineData(false, "skill is deleted")]
    public async Task ProjectSkillLink_IsRejectedWithoutWrite(
        bool linkFirst,
        string expectedReason
    )
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Rejected link project");
        var skillId = await CreateSkill(handler, "rejected-skill");
        var link = () => ExecuteAsUser(new AddSkillToProjectCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = skillId.Value
        });

        if (linkFirst)
            await link();
        else
            await DeleteSkill(handler, skillId);
        var writeCount = eventStore.WriteCount;

        var exception = await Assert.ThrowsAsync<EventValidationException>(link);

        Assert.Contains(expectedReason, exception.Message);
        Assert.Equal(writeCount, eventStore.WriteCount);
    }

    [Fact]
    public async Task ProjectDeletion_RemovesTheLinkFromEveryLinkedSkill()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Deleted linked project");
        var firstSkillId = await CreateSkill(handler, "first-skill");
        var secondSkillId = await CreateSkill(handler, "second-skill");
        await LinkSkill(handler, projectId, firstSkillId);
        await LinkSkill(handler, projectId, secondSkillId);

        await ExecuteAsUser(new DeleteProjectCommand(handler)
        {
            ProjectId = projectId.Value
        });

        Assert.True(ProjectState(eventStore, projectId).IsDeleted);
        Assert.All(
            [firstSkillId, secondSkillId],
            skillId => Assert.Empty(SkillState(eventStore, skillId).RelatedProjectIds)
        );
    }

    [Fact]
    public async Task SkillDeletion_RemovesTheLinkFromEveryLinkedProject()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var firstProjectId = await CreateProject(handler, "First linked project");
        var secondProjectId = await CreateProject(handler, "Second linked project");
        var skillId = await CreateSkill(handler, "deleted-linked-skill");
        await LinkSkill(handler, firstProjectId, skillId);
        await LinkSkill(handler, secondProjectId, skillId);

        await DeleteSkill(handler, skillId);

        Assert.True(SkillState(eventStore, skillId).IsDeleted);
        Assert.All(
            [firstProjectId, secondProjectId],
            projectId => Assert.Empty(ProjectState(eventStore, projectId).RelatedSkillIds)
        );
    }

    private static async Task<AggregateId> CreateProject(
        StateMachineHandler handler,
        string projectName
    )
    {
        var created = Assert.IsType<ProjectCreatedCommandResult>(
            await ExecuteAsUser(new CreateProjectCommand(handler)
            {
                ProjectName = projectName,
                ProjectDescription = "Project-skill link test.",
                RepositoryPaths = []
            })
        );

        return AggregateId.FromDatabaseGuid(created.ProjectId);
    }

    private static async Task<AggregateId> CreateSkill(
        StateMachineHandler handler,
        string name
    )
    {
        var skillId = AggregateId.New();
        await handler.ExecuteEvents(
            EventPayload.Create(
                Executor.Id,
                skillId,
                ProjectModule.Application.Constants.StateMachineIds.Skills,
                new SkillCreatedV3(
                    name,
                    "Description",
                    "Content",
                    [],
                    ImmutableDictionary<string, SkillReference2>.Empty,
                    Guid.Empty,
                    AggregateId.FromDatabaseGuid(
                        SharedModule.Constants.MemoryAggregateIds.User
                    )
                )
            )
        );

        return skillId;
    }

    private static Task<object> LinkSkill(
        StateMachineHandler handler,
        AggregateId projectId,
        AggregateId skillId
    ) =>
        ExecuteAsUser(new AddSkillToProjectCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = skillId.Value
        });

    private static Task<object> DeleteSkill(
        StateMachineHandler handler,
        AggregateId skillId
    )
    {
        var command = new DeleteSkillCommand(handler) { SkillId = skillId.Value };
        command.UseUserOrigin();
        return command.Execute(Executor);
    }

    private static ProjectStateData ProjectState(
        CapturingEventStoreWithOutbox eventStore,
        AggregateId projectId
    ) => Replay<ProjectStateData>(eventStore, projectId);

    private static SkillStateData SkillState(
        CapturingEventStoreWithOutbox eventStore,
        AggregateId skillId
    ) => Replay<SkillStateData>(eventStore, skillId);

    private static TState Replay<TState>(
        CapturingEventStoreWithOutbox eventStore,
        AggregateId aggregateId
    ) =>
        Assert.IsType<TState>(
            CreateCalculator()
                .Calculate(eventStore.GetStoredEvents(aggregateId), [])
                .StateData
        );

    private sealed class StubSkillSummaryRepository : ISkillSummaryRepository
    {
        public Task<List<SkillSummary>> List() => Task.FromResult(new List<SkillSummary>());

        public Task<SkillSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<SkillSummary?>(null);

        public Task<SkillSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new SkillSummarySearchResult([], 0));

        public Task<List<SkillUseScenarios>> GetUseScenarios(
            List<Guid> skillIds,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(new List<SkillUseScenarios>());
    }
}
