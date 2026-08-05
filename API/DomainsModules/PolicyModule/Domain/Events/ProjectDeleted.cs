using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectDeleted : IEvent;

public readonly record struct ProjectDeletedV1 : IProjectDeleted
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.IsDeleted = true;

        return projectPolicies;
    }
}
