using EventSourcing.Shared.Models;
using SharedModule.Constants;

namespace PolicyModule.Domain;

/// <summary>
/// Contains dictionary mapping repository absolute path to the project policies are for.
/// </summary>
public class RepositoryToProjectMapStateData : ISharedStateData
{
    public RepositoryToProjectMapStateData(AggregateId aggregateId)
    {
        _ = aggregateId;
    }

    public AggregateId Id { get; init; } =
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );
    public Dictionary<string, AggregateId> RepositoryToProjectMap { get; } =
        new Dictionary<string, AggregateId>();
    public bool IsDeleted
    {
        get => false;
        set { }
    }
}
