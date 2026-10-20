using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

/// <summary>
/// ID of a research discovery event. Discoveries replay into records of type
/// research, and the record keeps the same GUID.
/// </summary>
public readonly record struct FeatureResearchDiscoveryId(Guid Value)
{
    public static FeatureResearchDiscoveryId FromDatabaseGuid(Guid value) =>
        new(value);

    public FeatureRecordId ToRecordId() =>
        FeatureRecordId.FromDatabaseGuid(Value);
}
