namespace FeatureModule.API.Requests;

public sealed record RemoveRequirementsRequest
{
    public required List<Guid> RequirementIds { get; init; }
}
