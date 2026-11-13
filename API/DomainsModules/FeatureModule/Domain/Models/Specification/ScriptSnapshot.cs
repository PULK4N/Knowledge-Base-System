namespace FeatureModule.Domain.Models;

/// <summary>A saved copy of a Playwright script's code.</summary>
public sealed record ScriptSnapshot(string Content, DateTime UpdatedAt);
