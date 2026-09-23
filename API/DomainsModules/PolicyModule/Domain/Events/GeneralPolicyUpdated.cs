using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IGeneralPolicyUpdated : IEvent;

public readonly record struct GeneralPolicyUpdatedV1(Policy Policy) : IGeneralPolicyUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Policies[Policy.PolicyId] = Policy;

        return generalPoliciesStateData;
    }
}

public readonly record struct GeneralPolicyUpdatedV2(
    Policy Policy,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IGeneralPolicyUpdated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (GeneralPoliciesStateData)stateData;
        generalPoliciesStateData.Policies[Policy.PolicyId] = Policy;

        generalPoliciesStateData.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return generalPoliciesStateData;
    }
}
