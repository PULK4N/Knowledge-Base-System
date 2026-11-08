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
            Assert.DoesNotContain("UpdatedAt", json);
            var restored = (IEvent)JsonSerializer.Deserialize(json, type)!;
            Assert.Equal(json, JsonSerializer.Serialize(restored, type));
            var payload = EventPayload.Create(
                EventExecutor.FromDatabaseGuid(Guid.NewGuid()), AggregateId,
                "general-policies-state-machine", restored
            );

            restored.Apply(state, payload.EventExecutionInfo);
            legacy.Apply(legacyState, payload.EventExecutionInfo);
            Assert.Equal(json, JsonSerializer.Serialize(restored, type));

            var history = History(state);
            Assert.Equal(++expectedHistoryCount, history.Count);
            Assert.Equal(MemoryId, history.Last().AggregateId);
            Assert.Equal(type.Name, history.Last().EventName);
            Assert.Equal(payload.EventExecutionInfo.Timestamp, history.Last().Timestamp);
            Assert.Equal(type.Name.Contains("Policy") ? PolicyId : null, history.Last().PolicyId);
            Assert.Empty(History(legacyState));
            Assert.Equal(Snapshot(legacyState), Snapshot(state));
            Assert.Equal(state.Policies.Values.ToList(), legacyState.Policies.Values.ToList());
            if (state.Policies.ContainsKey(PolicyId))
                Assert.Equal(payload.EventExecutionInfo.Timestamp, state.Policies[PolicyId].UpdatedAt);
            foreach (var topic in state.Topics.Values)
            {
                Assert.Equal(payload.EventExecutionInfo.Timestamp, topic.UpdatedAt);
                Assert.Equal(topic.Policies.Values.ToList(), legacyState.Topics[topic.TopicName].Policies.Values.ToList());
                if (topic.Policies.ContainsKey(PolicyId))
                    Assert.Equal(payload.EventExecutionInfo.Timestamp, topic.Policies[PolicyId].UpdatedAt);
            }
            foreach (var family in state.AgentFamilies.Values)
            {
                Assert.Equal(payload.EventExecutionInfo.Timestamp, family.UpdatedAt);
                Assert.Equal(family.Policies.Values.ToList(), legacyState.AgentFamilies[family.AgentFamilyName].Policies.Values.ToList());
                if (family.Policies.ContainsKey(PolicyId))
                    Assert.Equal(payload.EventExecutionInfo.Timestamp, family.Policies[PolicyId].UpdatedAt);
            }
        }
        Assert.Equal(15, expectedHistoryCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Updating_a_container_preserves_its_policy_timestamps(bool family, bool legacy)
    {
        var state = new GeneralPoliciesStateData(AggregateId);
        var mutations = GeneralMutations();
        var offset = family ? 9 : 3;
        var timestamp = DateTime.UnixEpoch;
        foreach (var index in new List<int> { offset, offset + 2, offset + 1 })
        {
            var mutation = legacy ? mutations[index].Legacy : mutations[index].Current;
            mutation.Apply(state, new EventExecutionInfo
            {
                AggregateId = AggregateId,
                Timestamp = timestamp
            });
            timestamp = timestamp.AddMinutes(1);
        }

        var policies = family
            ? state.AgentFamilies[AgentFamilyName].Policies
            : state.Topics[TopicName].Policies;
        Assert.Equal(DateTime.UnixEpoch.AddMinutes(1), policies[PolicyId].UpdatedAt);
        Assert.Equal(DateTime.UnixEpoch.AddMinutes(2), family
            ? state.AgentFamilies[AgentFamilyName].UpdatedAt
            : state.Topics[TopicName].UpdatedAt);
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
