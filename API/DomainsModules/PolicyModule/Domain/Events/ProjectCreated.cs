using System.Collections.Immutable;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectCreated : IEvent;

public readonly record struct ProjectCreatedV1(
    string ProjectName,
    string ProjectDescription,
    ImmutableArray<string> RepositoryPaths
) : IProjectCreated
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var projectPolicies = (ProjectPoliciesStateData)stateData;
        projectPolicies.ProjectName = ProjectName;
        projectPolicies.ProjectDescription = ProjectDescription;
        projectPolicies.RepositoryPaths = RepositoryPaths.ToList();

        return projectPolicies;
    }
}
