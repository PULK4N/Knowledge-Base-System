namespace FeatureModule.Application.Commands;

public sealed record ScriptInput(
    string Path,
    List<Guid>? DefinitionOfDoneIds = null,
    string? SnapshotContent = null
);
