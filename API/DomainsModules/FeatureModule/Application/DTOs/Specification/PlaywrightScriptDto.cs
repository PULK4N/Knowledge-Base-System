using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>A script without its snapshot code; read the code with the snapshot query.</summary>
public sealed record PlaywrightScriptDto(
    Guid Id,
    string Path,
    List<Guid> DefinitionOfDoneIds,
    bool HasSnapshot,
    DateTime? SnapshotUpdatedAt,
    ItemOriginDto? Origin
)
{
    public static PlaywrightScriptDto FromModel(PlaywrightScript script) =>
        new(script.Id.Value, script.Path, script.DefinitionOfDoneIds.Select(id => id.Value).ToList(),
            script.Snapshot is not null, script.Snapshot?.UpdatedAt, ItemOriginDto.FromModel(script.Origin));
}
