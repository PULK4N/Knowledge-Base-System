using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record SetPlaywrightScriptSnapshotsRequest
{
    public required List<PlaywrightScriptSnapshotInput> Snapshots { get; init; }
}
