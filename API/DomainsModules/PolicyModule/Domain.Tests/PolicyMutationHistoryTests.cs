using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Tests;

public sealed class PolicyMutationHistoryTests
{
    private static readonly AggregateId AggregateId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly AggregateId MemoryId = AggregateId.FromDatabaseGuid(Guid.NewGuid());
    private static readonly Guid SessionId = Guid.NewGuid();
    private static readonly PolicyId PolicyId = new(Guid.NewGuid());
    private static readonly TopicName TopicName = new("topic");
    private static readonly AgentFamilyName AgentFamilyName = new("codex");
    private static readonly Policy Policy = new() { PolicyId = PolicyId, Title = "Policy", Description = "Description" };
    private static readonly Policy UpdatedPolicy = Policy with { Title = "Updated" };

    [Fact]
    public void All_mutations_round_trip_and_replay_with_the_same_transitions_and_memory_history()
    {
        var state = new GeneralPoliciesStateData(AggregateId);
        var legacyState = new GeneralPoliciesStateData(AggregateId);
        var expectedHistoryCount = 0;
        foreach (var (current, legacy) in GeneralMutations())
        {
            var type = current.GetType();
            var json = JsonSerializer.Serialize(current, type);
            var restored = (IEvent)JsonSerializer.Deserialize(json, type)!;
            Assert.Equal(json, JsonSerializer.Serialize(restored, type));
            var payload = EventPayload.Create(
                EventExecutor.FromDatabaseGuid(Guid.NewGuid()), AggregateId,
                "general-policies-state-machine", restored
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
        Assert.Equal(15, expectedHistoryCount);
    }

    private static List<(IEvent Current, IEvent Legacy)> GeneralMutations() =>
    [
        (new GeneralPolicyAddedV2(Policy, SessionId, MemoryId), new GeneralPolicyAddedV1(Policy)),
        (new GeneralPolicyUpdatedV2(UpdatedPolicy, SessionId, MemoryId), new GeneralPolicyUpdatedV1(UpdatedPolicy)),
        (new GeneralPolicyRemovedV2(PolicyId, SessionId, MemoryId), new GeneralPolicyRemovedV1(PolicyId)),
        (new TopicCreatedV2(TopicName, "Topic", SessionId, MemoryId), new TopicCreatedV1(TopicName, "Topic")),
        (new TopicUpdatedV2(TopicName, "Updated topic", SessionId, MemoryId), new TopicUpdatedV1(TopicName, "Updated topic")),
        (new TopicPolicyAddedV2(TopicName, Policy, SessionId, MemoryId), new TopicPolicyAddedV1(TopicName, Policy)),
        (new TopicPolicyUpdatedV2(TopicName, UpdatedPolicy, SessionId, MemoryId), new TopicPolicyUpdatedV1(TopicName, UpdatedPolicy)),
        (new TopicPolicyRemovedV2(TopicName, PolicyId, SessionId, MemoryId), new TopicPolicyRemovedV1(TopicName, PolicyId)),
        (new TopicRemovedV2(TopicName, SessionId, MemoryId), new TopicRemovedV1(TopicName)),
        (new AgentFamilyCreatedV2(AgentFamilyName, "Family", SessionId, MemoryId), new AgentFamilyCreatedV1(AgentFamilyName, "Family")),
        (new AgentFamilyUpdatedV2(AgentFamilyName, "Updated family", SessionId, MemoryId), new AgentFamilyUpdatedV1(AgentFamilyName, "Updated family")),
        (new AgentFamilyPolicyAddedV2(AgentFamilyName, Policy, SessionId, MemoryId), new AgentFamilyPolicyAddedV1(AgentFamilyName, Policy)),
        (new AgentFamilyPolicyUpdatedV2(AgentFamilyName, UpdatedPolicy, SessionId, MemoryId), new AgentFamilyPolicyUpdatedV1(AgentFamilyName, UpdatedPolicy)),
        (new AgentFamilyPolicyRemovedV2(AgentFamilyName, PolicyId, SessionId, MemoryId), new AgentFamilyPolicyRemovedV1(AgentFamilyName, PolicyId)),
        (new AgentFamilyRemovedV2(AgentFamilyName, SessionId, MemoryId), new AgentFamilyRemovedV1(AgentFamilyName))
    ];

    private static List<MemoryHistoryRecord> History(GeneralPoliciesStateData state) =>
        state.MemoryHistory;

    private static string Snapshot(GeneralPoliciesStateData general) =>
        JsonSerializer.Serialize(new
        {
            general.Id, general.IsDeleted, Policies = general.Policies.Values,
            Topics = general.Topics.Values.Select(topic => new
            {
                topic.TopicName, topic.Description, Policies = topic.Policies.Values
            }),
            AgentFamilies = general.AgentFamilies.Values.Select(family => new
            {
                family.AgentFamilyName, family.Description, Policies = family.Policies.Values
            })
        });
}
