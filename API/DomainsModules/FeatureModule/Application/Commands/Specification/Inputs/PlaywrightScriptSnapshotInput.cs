namespace FeatureModule.Application.Commands;

/// <summary>Null content removes the snapshot.</summary>
public sealed record PlaywrightScriptSnapshotInput(
    Guid Id,
    string? Content
);
