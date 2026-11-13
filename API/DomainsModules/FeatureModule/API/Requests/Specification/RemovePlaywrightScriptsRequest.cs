namespace FeatureModule.API.Requests;

public sealed record RemovePlaywrightScriptsRequest
{
    public required List<Guid> ScriptIds { get; init; }
}
