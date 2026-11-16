using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record AddScriptsRequest
{
    public required List<ScriptInput> Scripts { get; init; }
}
