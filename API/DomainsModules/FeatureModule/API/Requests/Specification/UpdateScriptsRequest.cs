using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record UpdateScriptsRequest
{
    public required List<ScriptChangeInput> Scripts { get; init; }
}
