using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using PolicyModule.Application.Commands;
using PolicyModule.Application.Models;
using PolicyModule.Domain;
using PolicyModule.Domain.Events;
using SharedModule.Constants;

namespace PolicyModule.Application.Tests;

public sealed partial class PolicyCommandTests
{
    private static readonly Guid SessionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    );

    [Theory]
    [InlineData("general", false, false)]
    [InlineData("topic", false, false)]
    [InlineData("agent-family", false, false)]
    [InlineData("project", false, false)]
    [InlineData("general", false, true)]
    [InlineData("general", true, false)]
    [InlineData("project", true, false)]
    public async Task Policy_mutations_record_origin_and_write_memory_in_the_same_batch(
        string scope, bool userOrigin, bool suppliedMemoryId
    )
    {
        var store = new CapturingEventStoreWithOutbox();
        if (!userOrigin)
            await SeedSession(store);
        var handler = CreateHandler(store);
        PolicyCommand command = scope switch
        {
            "general" => new AddGeneralPolicyCommand(handler) { Title = "Policy", Description = "Description" },
            "topic" => new CreateTopicCommand(handler) { TopicName = "topic", Description = "Description" },
            "agent-family" => new CreateAgentFamilyCommand(handler) { AgentFamilyName = "codex", Description = "Description" },
            _ => new CreateProjectCommand(handler)
            {
                ProjectName = "Project", ProjectDescription = "Description", RepositoryPaths = ["/workspace/project"]
            }
        };
        command.SessionId = SessionId;
        if (suppliedMemoryId)
            command.MemoryAggregateId = MemoryId.Value;
        if (userOrigin)
            command.UseUserOrigin();

        await command.Execute(Executor);

        Assert.Equal(1, store.WriteCount);
        var policyState = Assert.Single(store.LastWritten.Values,
            state => state.StateData is GeneralPoliciesStateData or ProjectPoliciesStateData);
        var payload = Assert.Single(policyState.LastExecutedPayloads);
        var history = policyState.StateData switch
        {
            GeneralPoliciesStateData general => Assert.Single(general.MemoryHistory),
            ProjectPoliciesStateData project => Assert.Single(project.MemoryHistory),
            _ => throw new InvalidOperationException()
        };
        var expectedMemoryId = userOrigin
            ? AggregateId.FromDatabaseGuid(SharedModule.Constants.MemoryAggregateIds.User)
            : MemoryId;
        Assert.Equal(expectedMemoryId, history.AggregateId);
        Assert.Equal(payload.EventExecutionInfo.EventName, history.EventName);
        Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Timestamp);
        Assert.Equal(userOrigin ? Guid.Empty : SessionId,
            (Guid)payload.EventData.GetType().GetProperty("SessionId")!.GetValue(payload.EventData)!);
        if (userOrigin)
        {
            Assert.DoesNotContain(store.LastWritten.Values, state => state.StateData is MemoryStateData);
        }
        else
        {
            var relation = Assert.IsType<MemoryRelationAddedV1>(Assert.Single(
                store.LastWritten[MemoryId].LastExecutedPayloads
            ).EventData);
            Assert.Equal(payload.EventExecutionInfo.AggregateId, relation.AggregateId);
            Assert.Equal(history.EventName, relation.Relation);
        }
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000")]
    [InlineData("dddddddd-dddd-dddd-dddd-dddddddddddd", "00000000-0000-0000-0000-000000000000")]
    [InlineData("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "dddddddd-dddd-dddd-dddd-dddddddddddd")]
    public async Task Missing_or_unrelated_memory_prevents_all_policy_writes(string sessionId, string memoryId)
    {
        var store = new CapturingEventStoreWithOutbox();
        await SeedSession(store);
        var command = new AddGeneralPolicyCommand(CreateHandler(store))
        {
            Title = "Policy", Description = "Description",
            SessionId = Guid.Parse(sessionId), MemoryAggregateId = Guid.Parse(memoryId)
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => command.Execute(Executor));

        Assert.Equal(0, store.WriteCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Project_repository_changes_and_deletion_keep_memory_in_the_conditional_batch(bool addRepository)
    {
        var store = new CapturingEventStoreWithOutbox();
        await SeedSession(store);
        var handler = CreateHandler(store);
        var created = Assert.IsType<ProjectCreatedCommandResult>(await ExecuteAsUser(new CreateProjectCommand(handler)
        {
            ProjectName = "Project", ProjectDescription = "Description",
            RepositoryPaths = ["/workspace/first", "/workspace/second"]
        }));
        PolicyCommand command = addRepository
            ? new AddRepositoryToProjectCommand(handler) { ProjectId = created.ProjectId, RepositoryPath = "/workspace/third" }
            : new DeleteProjectCommand(handler) { ProjectId = created.ProjectId };
        command.SessionId = SessionId;

        await command.Execute(Executor);

        Assert.Equal(2, store.WriteCount);
        Assert.Equal(3, store.LastWritten.Count);
        var projectId = AggregateId.FromDatabaseGuid(created.ProjectId);
        var project = Assert.IsType<ProjectPoliciesStateData>(store.LastWritten[projectId].StateData);
        var memoryRelation = Assert.IsType<MemoryRelationAddedV1>(Assert.Single(store.LastWritten[MemoryId].LastExecutedPayloads).EventData);
        Assert.Equal(projectId, memoryRelation.AggregateId);
        Assert.Equal(addRepository ? nameof(RepositoryAddedToProjectV2) : nameof(ProjectDeletedV2), memoryRelation.Relation);
        Assert.Equal(MemoryId, project.MemoryHistory.Last().AggregateId);
        Assert.Equal(!addRepository, project.IsDeleted);
        var mapState = store.LastWritten[AggregateId.FromDatabaseGuid(StateDataAggregateIds.RepositoryToProjectMap)];
        Assert.Equal(addRepository ? 1 : 2, mapState.LastExecutedPayloads.Count);
        var map = Assert.IsType<RepositoryToProjectMapStateData>(mapState.StateData);
        Assert.Equal(addRepository ? 3 : 0, map.RepositoryToProjectMap.Count);
    }

    [Fact]
    public async Task Repository_mapping_failure_does_not_persist_policy_or_memory_events()
    {
        var store = new CapturingEventStoreWithOutbox();
        await SeedSession(store);
        var handler = CreateHandler(store);
        await ExecuteAsUser(new CreateProjectCommand(handler)
        {
            ProjectName = "First", ProjectDescription = "Description", RepositoryPaths = ["/workspace/claimed"]
        });
        var target = Assert.IsType<ProjectCreatedCommandResult>(await ExecuteAsUser(new CreateProjectCommand(handler)
        {
            ProjectName = "Target", ProjectDescription = "Description"
        }));
        var targetId = AggregateId.FromDatabaseGuid(target.ProjectId);
        var command = new AddRepositoryToProjectCommand(handler)
        {
            ProjectId = target.ProjectId, RepositoryPath = "/workspace/claimed", SessionId = SessionId
        };

        await Assert.ThrowsAsync<EventValidationException>(() => command.Execute(Executor));

        Assert.Equal(2, store.WriteCount);
        Assert.Single(store.GetStoredEvents(targetId));
        Assert.Empty(store.GetStoredEvents(MemoryId));
    }

    private static Task SeedSession(CapturingEventStoreWithOutbox store)
    {
        var payload = EventPayload.Create(
            Executor.Id, MemoryModule.Domain.MemoryAggregateIds.SessionAggregateMap,
            MemoryModule.Application.Constants.StateMachineIds.SessionAggregateMap,
            new SessionAggregateMapAddedV1(new ThreadId(SessionId), MemoryId)
        );
        payload.EventExecutionInfo.OrderNumber = 1;
        return store.Write(new List<EventPayload> { payload });
    }
}
