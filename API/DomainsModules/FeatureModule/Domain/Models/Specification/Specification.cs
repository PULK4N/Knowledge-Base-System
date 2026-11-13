namespace FeatureModule.Domain.Models;

/// <summary>
/// Requirements, definitions of done and Playwright scripts of one plan or of
/// the feature. Links point only to items of the same specification.
/// </summary>
public sealed class Specification
{
    public List<Requirement> Requirements { get; set; } = [];

    public List<DefinitionOfDone> DefinitionsOfDone { get; set; } = [];

    public List<PlaywrightScript> PlaywrightScripts { get; set; } = [];

    /// <summary>Highest number ever used per type; removed numbers stay retired.</summary>
    public Dictionary<RequirementType, int> LastRequirementNumbers { get; set; } = [];

    public int LastDefinitionOfDoneNumber { get; set; }

    public int LastRequirementNumber(RequirementType type) =>
        LastRequirementNumbers.GetValueOrDefault(type);

    public void UseRequirementNumber(RequirementType type, int number) =>
        LastRequirementNumbers[type] = Math.Max(LastRequirementNumber(type), number);

    public void UseDefinitionOfDoneNumber(int number) =>
        LastDefinitionOfDoneNumber = Math.Max(LastDefinitionOfDoneNumber, number);

    public void RemoveRequirements(IReadOnlyCollection<RequirementId> ids)
    {
        Requirements.RemoveAll(requirement => ids.Contains(requirement.Id));
        foreach (var definitionOfDone in DefinitionsOfDone)
            definitionOfDone.RequirementIds.RemoveAll(ids.Contains);
    }

    public void RemoveDefinitionsOfDone(IReadOnlyCollection<DefinitionOfDoneId> ids)
    {
        DefinitionsOfDone.RemoveAll(definitionOfDone => ids.Contains(definitionOfDone.Id));
        foreach (var script in PlaywrightScripts)
            script.DefinitionOfDoneIds.RemoveAll(ids.Contains);
    }
}
