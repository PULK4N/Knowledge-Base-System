namespace FeatureModule.Domain.Models;

/// <summary>
/// How to confirm that work is done, shown as DOD-n, with the requirements it fulfils.
/// </summary>
public sealed class DefinitionOfDone
{
    public DefinitionOfDoneId Id { get; init; }

    public int Number { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<RequirementId> RequirementIds { get; set; } = [];

    public DefinitionOfDoneState State { get; set; }

    /// <summary>How it was checked, or why it could not be checked yet.</summary>
    public string StateDescription { get; set; } = string.Empty;

    public DateTime? StateCheckedAt { get; set; }

    public ItemOrigin? Origin { get; set; }
}
