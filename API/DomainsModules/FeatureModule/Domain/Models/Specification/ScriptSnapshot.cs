namespace FeatureModule.Domain.Models;

/// <summary>A saved copy of a script's code.</summary>
public sealed record ScriptSnapshot(string Content, DateTime UpdatedAt);
