using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record AddPlaywrightScriptsRequest
{
    public required List<PlaywrightScriptInput> Scripts { get; init; }
}
