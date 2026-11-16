namespace FeatureModule.Application.DTOs;

public sealed record ScriptSnapshotDto(Guid ScriptId, string Path, string Content, DateTime UpdatedAt);
