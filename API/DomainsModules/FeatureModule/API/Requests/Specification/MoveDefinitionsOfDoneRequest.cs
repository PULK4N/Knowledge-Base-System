namespace FeatureModule.API.Requests;

public sealed record MoveDefinitionsOfDoneRequest
{
    public required Guid TargetPlanId { get; init; }

    public required List<Guid> DefinitionOfDoneIds { get; init; }
}
