using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record SetScriptSnapshotsRequest
{
    public required List<ScriptSnapshotInput> Snapshots { get; init; }
}
