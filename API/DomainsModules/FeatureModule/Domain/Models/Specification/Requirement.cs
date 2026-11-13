namespace FeatureModule.Domain.Models;

/// <summary>
/// A requirement shown to people as its type prefix and number, such as FR-3.
/// Numbers are never reused within the specification that owns it.
/// </summary>
public sealed class Requirement
{
    public RequirementId Id { get; init; }

    public RequirementType Type { get; set; }

    public int Number { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>ID in an outside system such as Confluence; not unique.</summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Where the requirement comes from, as a link or text.</summary>
    public string Source { get; set; } = string.Empty;

    public ItemOrigin? Origin { get; set; }
}
