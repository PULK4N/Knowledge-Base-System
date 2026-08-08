using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IRepositoryAddedToProject : IEvent;

public readonly record struct RepositoryAddedToProjectV1(
    string RepositoryPath
) : IRepositoryAddedToProject
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var project = (ProjectPoliciesStateData)stateData;
        project.RepositoryPaths.Add(RepositoryPath);

        return project;
    }
}
