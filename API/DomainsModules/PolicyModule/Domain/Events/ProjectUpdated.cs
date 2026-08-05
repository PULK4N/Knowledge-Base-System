using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

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
