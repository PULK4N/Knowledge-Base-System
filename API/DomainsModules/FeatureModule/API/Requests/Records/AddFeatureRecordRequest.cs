namespace FeatureModule.API.Requests;

public sealed record AddFeatureRecordRequest : FeatureRecordContentRequest
{
    public List<Guid> PlanIds { get; init; } = [];
}
