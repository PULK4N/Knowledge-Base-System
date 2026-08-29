using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Domain;

public class GeneralPoliciesStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } =
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );
    public bool IsDeleted { get; set; }
    public Dictionary<PolicyId, Policy> Policies { get; } = new Dictionary<PolicyId, Policy>();
    public Dictionary<TopicName, Topic> Topics { get; } = new Dictionary<TopicName, Topic>();
    public Dictionary<AgentFamilyName, AgentFamily> AgentFamilies { get; } =
        new Dictionary<AgentFamilyName, AgentFamily>();
}
