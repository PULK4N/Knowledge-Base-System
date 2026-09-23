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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_mutations_round_trip_and_replay_with_the_same_transitions_and_memory_history(bool project)
    {
        var state = project ? (object)new ProjectPoliciesStateData(AggregateId) : new GeneralPoliciesStateData(AggregateId);
        var legacyState = project ? (object)new ProjectPoliciesStateData(AggregateId) : new GeneralPoliciesStateData(AggregateId);
        var expectedHistoryCount = 0;
        foreach (var (current, legacy) in project ? ProjectMutations() : GeneralMutations())
        {
            var type = current.GetType();
            var json = JsonSerializer.Serialize(current, type);
            var restored = (IEvent)JsonSerializer.Deserialize(json, type)!;
            Assert.Equal(json, JsonSerializer.Serialize(restored, type));
            var payload = EventPayload.Create(
                EventExecutor.FromDatabaseGuid(Guid.NewGuid()), AggregateId,
                project ? "project-policies-state-machine" : "general-policies-state-machine", restored
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
        Assert.Equal(project ? 9 : 15, expectedHistoryCount);
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

    private static List<MemoryHistoryRecord> History(object state) => state switch
    {
        GeneralPoliciesStateData general => general.MemoryHistory,
        ProjectPoliciesStateData project => project.MemoryHistory,
        _ => throw new InvalidOperationException()
    };

    private static string Snapshot(object state) => state switch
    {
        GeneralPoliciesStateData general => JsonSerializer.Serialize(new
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
        }),
        ProjectPoliciesStateData project => JsonSerializer.Serialize(new
        {
            project.Id, project.IsDeleted, project.ProjectName, project.ProjectDescription,
            project.RepositoryPaths, project.RelatedTopics, Policies = project.Policies.Values
        }),
        _ => throw new InvalidOperationException()
    };
}
