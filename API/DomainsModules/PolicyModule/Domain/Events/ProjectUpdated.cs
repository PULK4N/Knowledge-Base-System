using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

using PolicyModule.Domain.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectUpdated : IEvent;

public readonly record struct ProjectUpdatedV1(
    string ProjectName,
    string ProjectDescription
) : IProjectUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.ProjectName = ProjectName;
        projectPolicies.ProjectDescription = ProjectDescription;

        return projectPolicies;
    }
}

public readonly record struct ProjectUpdatedV2(
    string ProjectName,
    string ProjectDescription,
    Guid SessionId,
    AggregateId MemoryAggregateId
) : IProjectUpdated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.ProjectName = ProjectName;
        projectPolicies.ProjectDescription = ProjectDescription;

        projectPolicies.MemoryHistory.Add(
            new MemoryHistoryRecord(
                eventExecutionInfo.EventName,
                eventExecutionInfo.Timestamp,
                MemoryAggregateId
            )
        );
        return projectPolicies;
    }
}
