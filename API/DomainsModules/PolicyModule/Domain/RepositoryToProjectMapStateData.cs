using System.Data;
using EventSourcing.Shared.Models;

namespace PolicyModule.Domain;

/// <summary>
/// Contains dictionary mapping repository absolute path to the project policies are for.
/// </summary>
public class RepositoryToProjectMapStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public Dictionary<string, AggregateId> RepositoryToProjectMap { get; } =
        new Dictionary<string, AggregateId>();
    public bool IsDeleted
    {
        get => throw new ConstraintException();
        set => throw new ConstraintException();
    }
}
