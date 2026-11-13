namespace FeatureModule.API.Requests;

public sealed record RemoveDefinitionsOfDoneRequest
{
    public required List<Guid> DefinitionOfDoneIds { get; init; }
}
