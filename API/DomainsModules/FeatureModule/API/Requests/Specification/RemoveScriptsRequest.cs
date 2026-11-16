namespace FeatureModule.API.Requests;

public sealed record RemoveScriptsRequest
{
    public required List<Guid> ScriptIds { get; init; }
}
