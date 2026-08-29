using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyPolicyRemoved : IEvent;

public readonly record struct AgentFamilyPolicyRemovedV1(
    AgentFamilyName AgentFamilyName,
    PolicyId PolicyId
) : IAgentFamilyPolicyRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies[AgentFamilyName].Policies.Remove(
            PolicyId
        );

        return generalPolicies;
    }
}
