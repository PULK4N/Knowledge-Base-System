namespace FeatureModule.API.Requests;

public sealed record UpdateFeatureRecordRequest : FeatureRecordContentRequest
{
    public required Guid RecordId { get; init; }
}
