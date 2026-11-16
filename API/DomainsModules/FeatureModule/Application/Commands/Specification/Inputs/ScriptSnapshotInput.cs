namespace FeatureModule.Application.Commands;

/// <summary>Null content removes the snapshot.</summary>
public sealed record ScriptSnapshotInput(
    Guid Id,
    string? Content
);
