using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>New snapshot code of a script; null removes the snapshot.</summary>
public sealed record PlaywrightScriptSnapshotEntry(
    PlaywrightScriptId Id,
    string? Content
);
