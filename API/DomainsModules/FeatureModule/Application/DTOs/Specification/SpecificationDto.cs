using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>
/// Requirements, definitions of done and scripts with readable warnings about
/// missing links, such as "FR-2 has no definition of done".
/// </summary>
public sealed record SpecificationDto(
    List<RequirementDto> Requirements,
    List<DefinitionOfDoneDto> DefinitionsOfDone,
    List<PlaywrightScriptDto> PlaywrightScripts,
    List<string> Warnings
)
{
    public static SpecificationDto FromModel(Specification specification) =>
        new(specification.Requirements
                .OrderBy(item => item.Type).ThenBy(item => item.Number)
                .Select(RequirementDto.FromModel).ToList(),
            specification.DefinitionsOfDone
                .OrderBy(item => item.Number)
                .Select(DefinitionOfDoneDto.FromModel).ToList(),
            specification.PlaywrightScripts.Select(PlaywrightScriptDto.FromModel).ToList(),
            CollectWarnings(specification));

    private static List<string> CollectWarnings(Specification specification)
    {
        var fulfilled = specification.DefinitionsOfDone.SelectMany(item => item.RequirementIds).ToHashSet();
        var confirmed = specification.PlaywrightScripts.SelectMany(item => item.DefinitionOfDoneIds).ToHashSet();
        return specification.Requirements
            .OrderBy(item => item.Type).ThenBy(item => item.Number)
            .Where(item => !fulfilled.Contains(item.Id))
            .Select(item => $"{SpecificationLabels.Requirement(item.Type, item.Number)} has no definition of done")
            .Concat(specification.DefinitionsOfDone.OrderBy(item => item.Number).SelectMany(item =>
                new[]
                {
                    item.RequirementIds.Count == 0 ? $"{SpecificationLabels.DefinitionOfDone(item.Number)} fulfils no requirement" : null,
                    confirmed.Contains(item.Id) ? null : $"{SpecificationLabels.DefinitionOfDone(item.Number)} has no Playwright script"
                }.OfType<string>()))
            .Concat(specification.PlaywrightScripts
                .Where(item => item.DefinitionOfDoneIds.Count == 0)
                .Select(item => $"{item.Path} confirms no definition of done"))
            .ToList();
    }
}
