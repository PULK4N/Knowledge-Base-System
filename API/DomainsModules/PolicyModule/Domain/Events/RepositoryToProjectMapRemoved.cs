using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IRepositoryToProjectMapRemoved : IEvent;

public readonly record struct RepositoryToProjectMapRemovedV1(
    string RepositoryPath,
    AggregateId ProjectAggregateId
) : IRepositoryToProjectMapRemoved
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var repositoryMap = (RepositoryToProjectMapStateData)stateData;
        repositoryMap.RepositoryToProjectMap.Remove(RepositoryPath);

        return repositoryMap;
    }
}
