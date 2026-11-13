namespace FeatureModule.Application.DTOs;

public sealed record PlaywrightScriptSnapshotDto(Guid ScriptId, string Path, string Content, DateTime UpdatedAt);
