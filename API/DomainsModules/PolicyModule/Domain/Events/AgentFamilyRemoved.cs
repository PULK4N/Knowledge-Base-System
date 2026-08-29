using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyRemoved : IEvent;

public readonly record struct AgentFamilyRemovedV1(
    AgentFamilyName AgentFamilyName
) : IAgentFamilyRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies.Remove(AgentFamilyName);

        return generalPolicies;
    }
}
