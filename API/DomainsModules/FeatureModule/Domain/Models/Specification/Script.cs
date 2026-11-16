namespace FeatureModule.Domain.Models;

/// <summary>
/// A script path with the definitions of done it confirms. Paths need not be unique.
/// </summary>
public sealed class Script
{
    public ScriptId Id { get; init; }

    public string Path { get; set; } = string.Empty;

    public List<DefinitionOfDoneId> DefinitionOfDoneIds { get; set; } = [];

    public ScriptSnapshot? Snapshot { get; set; }

    public ItemOrigin? Origin { get; set; }
}
