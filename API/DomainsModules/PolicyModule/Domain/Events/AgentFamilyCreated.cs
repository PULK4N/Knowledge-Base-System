using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IAgentFamilyCreated : IEvent;

public readonly record struct AgentFamilyCreatedV1(
    AgentFamilyName AgentFamilyName,
    string Description
) : IAgentFamilyCreated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies.Add(
            AgentFamilyName,
            new AgentFamily
            {
                AgentFamilyName = AgentFamilyName,
                Description = Description
            }
        );

        return generalPolicies;
    }
}

public readonly record struct AgentFamilyCreatedV2(
    AgentFamilyName AgentFamilyName,
    string Description,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IAgentFamilyCreated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var generalPolicies = (GeneralPoliciesStateData)stateData;
        generalPolicies.AgentFamilies.Add(
            AgentFamilyName,
            new AgentFamily
            {
                AgentFamilyName = AgentFamilyName,
                Description = Description
            }
        );

        generalPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return generalPolicies;
    }
}
