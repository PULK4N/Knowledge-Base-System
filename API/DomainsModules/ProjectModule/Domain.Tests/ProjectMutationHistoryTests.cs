using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;

namespace ProjectModule.Domain.Tests;

public sealed class ProjectMutationHistoryTests
{
    private static readonly AggregateId AggregateId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly PolicyId PolicyId = new(Guid.NewGuid());
    private static readonly TopicName TopicName = new("topic");
    private static readonly Policy Policy = new() { PolicyId = PolicyId, Title = "Policy", Description = "Description" };
    private static readonly Policy UpdatedPolicy = Policy with { Title = "Updated" };

    [Fact]
    public void All_mutations_round_trip_and_replay_with_the_same_transitions_and_memory_history()
    {
        var state = new ProjectStateData(AggregateId);
        var legacyState = new ProjectStateData(AggregateId);
        var expectedHistoryCount = 0;
        foreach (var (current, legacy) in ProjectMutations())
        {
            var type = current.GetType();
            var json = JsonSerializer.Serialize(current, type);
            var restored = (IEvent)JsonSerializer.Deserialize(json, type)!;
            Assert.Equal(json, JsonSerializer.Serialize(restored, type));
            var payload = EventPayload.Create(
                EventExecutor.FromDatabaseGuid(Guid.NewGuid()), AggregateId,
                "project-state-machine", restored
            );

            restored.Apply(state, payload.EventExecutionInfo);
            legacy.Apply(legacyState, payload.EventExecutionInfo);

            var history = History(state);
            Assert.Equal(++expectedHistoryCount, history.Count);
            Assert.Equal(MemoryId, history.Last().AggregateId);
            Assert.Equal(type.Name, history.Last().EventName);
            Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Last().Timestamp);
            Assert.Equal(type.Name.Contains("Policy") ? PolicyId : null, history.Last().PolicyId);
            Assert.Empty(History(legacyState));
            Assert.Equal(Snapshot(legacyState), Snapshot(state));
        }
        Assert.Equal(9, expectedHistoryCount);
    }

    private static List<(IEvent Current, IEvent Legacy)> ProjectMutations() =>
    [
        (new ProjectCreatedV2("Project", "Description", ["/workspace/first"], SessionId, MemoryId), new ProjectCreatedV1("Project", "Description", ["/workspace/first"])),
        (new ProjectUpdatedV2("Updated", "Updated description", SessionId, MemoryId), new ProjectUpdatedV1("Updated", "Updated description")),
        (new RepositoryAddedToProjectV2("/workspace/second", SessionId, MemoryId), new RepositoryAddedToProjectV1("/workspace/second")),
        (new ProjectPolicyAddedV2(Policy, SessionId, MemoryId), new ProjectPolicyAddedV1(Policy)),
        (new ProjectPolicyUpdatedV2(UpdatedPolicy, SessionId, MemoryId), new ProjectPolicyUpdatedV1(UpdatedPolicy)),
        (new ProjectPolicyRemovedV2(PolicyId, SessionId, MemoryId), new ProjectPolicyRemovedV1(PolicyId)),
        (new TopicRelationAddedToProjectV2(TopicName, SessionId, MemoryId), new TopicRelationAddedToProjectV1(TopicName)),
        (new TopicRelationRemovedFromProjectV2(TopicName, SessionId, MemoryId), new TopicRelationRemovedFromProjectV1(TopicName)),
        (new ProjectDeletedV2(SessionId, MemoryId), new ProjectDeletedV1())
    ];

    private static List<MemoryHistoryRecord> History(ProjectStateData state) =>
        state.MemoryHistory;

    private static string Snapshot(ProjectStateData project) =>
        JsonSerializer.Serialize(new
        {
            project.Id, project.IsDeleted, project.ProjectName, project.ProjectDescription,
            project.RepositoryPaths, project.RelatedTopics, Policies = project.Policies.Values
        });
}
