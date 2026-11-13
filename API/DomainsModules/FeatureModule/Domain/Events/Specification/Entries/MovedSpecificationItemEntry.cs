namespace FeatureModule.Domain.Events;

/// <summary>A moved item's ID and its new number in the target plan.</summary>
public sealed record MovedSpecificationItemEntry<TId>(TId Id, int Number);
