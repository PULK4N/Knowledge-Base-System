using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Events;

public interface IProjectPolicyRemoved : IEvent;

public readonly record struct ProjectPolicyRemovedV1(PolicyId PolicyId) : IProjectPolicyRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (ProjectStateData)stateData;
        generalPoliciesStateData.Policies.Remove(PolicyId);

        return generalPoliciesStateData;
    }
}

public readonly record struct ProjectPolicyRemovedV2(
    PolicyId PolicyId,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectPolicyRemoved
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var generalPoliciesStateData = (ProjectStateData)stateData;
        generalPoliciesStateData.Policies.Remove(PolicyId);

        generalPoliciesStateData.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId,
                PolicyId
            )
        );
        return generalPoliciesStateData;
    }
}
