using EventSourcing.Shared.Exceptions;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using ProjectModule.Application.Commands;
using ProjectModule.Application.Models;
using ProjectModule.Domain;
using ProjectModule.Domain.Events;
using SharedModule.Constants;

namespace ProjectModule.Application.Tests;

public sealed partial class ProjectCommandTests
{
    private static readonly Guid SessionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
    );

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Project_mutations_record_origin_and_write_memory_in_the_same_batch(
        bool userOrigin
    )
    {
        var store = new CapturingEventStoreWithOutbox();
        if (!userOrigin)
            await SeedSession(store);
        var command = new CreateProjectCommand(CreateHandler(store))
        {
            ProjectName = "Project", ProjectDescription = "Description", RepositoryPaths = ["/workspace/project"],
            SessionId = SessionId
        };
        if (userOrigin)
            command.UseUserOrigin();

        await command.Execute(Executor);

        Assert.Equal(1, store.WriteCount);
        var projectState = Assert.Single(store.LastWritten.Values,
            state => state.StateData is ProjectStateData);
        var payload = Assert.Single(projectState.LastExecutedPayloads);
        var history = Assert.Single(((ProjectStateData)projectState.StateData).MemoryHistory);
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
        ProjectCommand command = addRepository
            ? new AddRepositoryToProjectCommand(handler) { ProjectId = created.ProjectId, RepositoryPath = "/workspace/third" }
            : new DeleteProjectCommand(handler) { ProjectId = created.ProjectId };
        command.SessionId = SessionId;

        await command.Execute(Executor);

        Assert.Equal(2, store.WriteCount);
        Assert.Equal(3, store.LastWritten.Count);
        var projectId = AggregateId.FromDatabaseGuid(created.ProjectId);
        var project = Assert.IsType<ProjectStateData>(store.LastWritten[projectId].StateData);
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
        var stateInfo = StateInfo.Create(
            new object(),
            payload.EventExecutionInfo.StateMachineId,
            payload.EventExecutionInfo.AggregateId
        );
        stateInfo.LastExecutedPayloads = [payload];
        return store.Write(new List<StateInfo> { stateInfo });
    }
}
