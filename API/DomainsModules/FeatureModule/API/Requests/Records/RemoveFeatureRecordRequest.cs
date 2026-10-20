namespace FeatureModule.API.Requests;

public sealed record RemoveFeatureRecordRequest
{
    public required Guid RecordId { get; init; }
}
