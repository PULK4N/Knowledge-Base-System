using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record UpdatePlaywrightScriptsRequest
{
    public required List<PlaywrightScriptChangeInput> Scripts { get; init; }
}
