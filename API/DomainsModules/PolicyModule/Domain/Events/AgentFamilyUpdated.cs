using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyUpdated : IEvent;

public readonly record struct AgentFamilyUpdatedV1(
    AgentFamilyName AgentFamilyName,
    string Description
) : IAgentFamilyUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        var existingAgentFamily = generalPolicies.AgentFamilies[
            AgentFamilyName
        ];
        var updatedAgentFamily = new AgentFamily
        {
            AgentFamilyName = AgentFamilyName,
            Description = Description
        };

        foreach (var policy in existingAgentFamily.Policies)
            updatedAgentFamily.Policies.Add(policy.Key, policy.Value);

        generalPolicies.AgentFamilies[AgentFamilyName] =
            updatedAgentFamily;

        return generalPolicies;
    }
}
