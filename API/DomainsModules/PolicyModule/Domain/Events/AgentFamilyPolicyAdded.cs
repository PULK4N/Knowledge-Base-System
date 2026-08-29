using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyPolicyAdded : IEvent;

public readonly record struct AgentFamilyPolicyAddedV1(
    AgentFamilyName AgentFamilyName,
    Policy Policy
) : IAgentFamilyPolicyAdded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies.Add(
            Policy.PolicyId,
            Policy
        );

        return generalPolicies;
    }
}
