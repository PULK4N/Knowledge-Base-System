using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain.Events;

public interface IRepositoryToProjectMapAdded : IEvent;

public readonly record struct RepositoryToProjectMapAddedV1(string RepositoryPath, AggregateId AggregateId)
    : IRepositoryToProjectMapAdded
{
    public object Apply(object stateData, EventExecutionInfo eventExecutionInfo)
    {
        var repositoryToProjectMapStateData = (RepositoryToProjectMapStateData)stateData;
        repositoryToProjectMapStateData.RepositoryToProjectMap.Add(RepositoryPath, AggregateId);

        return repositoryToProjectMapStateData;
    }
}
