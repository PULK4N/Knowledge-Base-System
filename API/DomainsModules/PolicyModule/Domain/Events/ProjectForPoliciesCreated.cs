using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IProjectForPoliciesCreated : IEvent;

public readonly record struct ProjectForPoliciesCreatedV1(
    string ProjectName,
    string ProjectDescripton,
    List<string> ProjectRepositories
) : IProjectForPoliciesCreated
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var projectPoliciesStateData = (ProjectPoliciesStateData)stateData;
        projectPoliciesStateData.ProjectName = ProjectName;
        projectPoliciesStateData.ProjectDescription = ProjectDescripton;
        projectPoliciesStateData.ProjectRepositories = ProjectRepositories;

        return projectPoliciesStateData;
    }
}
