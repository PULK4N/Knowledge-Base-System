using EventSourcing.Shared.Models;

namespace OutboxProcessingModule.Tests.TestModels;

public sealed class AccountStateData(AggregateId aggregateId) : ISharedStateData
{
    public float Money { get; set; }
    public AggregateId Id { get; init; } = aggregateId;
    public bool IsDeleted { get; set; }
}
