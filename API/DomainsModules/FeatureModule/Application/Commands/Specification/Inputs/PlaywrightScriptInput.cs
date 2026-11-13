namespace FeatureModule.Application.Commands;

public sealed record PlaywrightScriptInput(
    string Path,
    List<Guid>? DefinitionOfDoneIds = null,
    string? SnapshotContent = null
);
