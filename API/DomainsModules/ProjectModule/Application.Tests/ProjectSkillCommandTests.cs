using ProjectModule.Shared;
using System.Collections.Immutable;
using EventSourcing.Core;
using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using ProjectModule.Application.Commands;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Application.DTOs;
using ProjectModule.Application.Queries;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;
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
            Assert.IsType<ProjectSkillRelationAddedV2>(
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

    [Fact]
    public async Task ProjectReview_ListsReviewSkillsInLinkOrderWithLevelModels()
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Reviewed project");
        var angularId = await CreateSkill(handler, "angular-code-writter");
        var planId = await CreateSkill(handler, "plan-skill");
        var skippedId = await CreateSkill(handler, "skipped-skill");

        await ExecuteAsUser(new AddSkillToProjectCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = angularId.Value,
            UsedForReview = true,
            ReviewNotes = "  Focus on RxJS.  ",
            ReviewLevel = ReviewLevel.Heavy
        });
        await LinkSkill(handler, projectId, skippedId);
        await LinkSkill(handler, projectId, planId);
        await ExecuteAsUser(new UpdateProjectSkillCommand(handler)
        {
            ProjectId = projectId.Value,
            SkillId = planId.Value,
            UsedForReview = true,
            ReviewLevel = ReviewLevel.Medium
        });
        await ExecuteAsUser(new SetProjectReviewLevelModelsCommand(handler)
        {
            ProjectId = projectId.Value,
            LevelModels = new()
            {
                [ReviewLevel.Medium] = new ReviewModel(" gpt-6.1-sol ", "high")
            }
        });

        var review = Assert.IsType<ProjectReviewDto>(
            await new GetProjectReviewQuery(
                CreateCalculator(),
                eventStore,
                new NamedSkillSummaryRepository(
                    [
                        new(angularId.Value, "angular-code-writter", []),
                        new(planId.Value, "plan-skill", []),
                        new(skippedId.Value, "skipped-skill", [])
                    ]
                )
            ) { ProjectId = projectId.Value }.Execute(Executor)
        );

        Assert.Equal(
            new ReviewModel("gpt-6.1-sol", "high"),
            Assert.Single(review.LevelModels).Value
        );
        Assert.Equal(
            [
                new ProjectReviewSkillDto(angularId.Value, "angular-code-writter", ReviewLevel.Heavy, "Focus on RxJS."),
                new ProjectReviewSkillDto(planId.Value, "plan-skill", ReviewLevel.Medium, "")
            ],
            review.ReviewSkills
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReviewSkillWithoutLevel_IsRejectedWithoutWrite(bool update)
    {
        var eventStore = new CapturingEventStoreWithOutbox();
        var handler = CreateHandler(eventStore);
        var projectId = await CreateProject(handler, "Level check project");
        var skillId = await CreateSkill(handler, "level-check-skill");
        if (update)
            await LinkSkill(handler, projectId, skillId);
        var writeCount = eventStore.WriteCount;

        var exception = await Assert.ThrowsAsync<EventValidationException>(
            () => update
                ? ExecuteAsUser(new UpdateProjectSkillCommand(handler)
                {
                    ProjectId = projectId.Value,
                    SkillId = skillId.Value,
                    UsedForReview = true
                })
                : ExecuteAsUser(new AddSkillToProjectCommand(handler)
                {
                    ProjectId = projectId.Value,
                    SkillId = skillId.Value,
                    UsedForReview = true
                })
        );

        Assert.Contains("needs a review level", exception.Message);
        Assert.Equal(writeCount, eventStore.WriteCount);
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

    private sealed class NamedSkillSummaryRepository(
        List<SkillUseScenarios> skills
    ) : ISkillSummaryRepository
    {
        public Task<List<SkillSummary>> List() => throw new NotSupportedException();

        public Task<SkillSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<SkillSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<SkillUseScenarios>> GetUseScenarios(
            List<Guid> skillIds,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                skills
                    .Where(skill => skillIds.Contains(skill.SkillId))
                    .OrderBy(skill => skill.Name, StringComparer.Ordinal)
                    .ToList()
            );
    }

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
