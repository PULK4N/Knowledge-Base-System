namespace PolicyModule.Domain.Models;

/// <summary>
/// Identifies an agent family such as "claude" or "codex". Names are normalized
/// at the application boundary so a family created as "Claude" still matches the
/// "claude" an agent hook reports for itself.
/// </summary>
public readonly record struct AgentFamilyName(string Name)
{
    public static AgentFamilyName Normalized(string name) =>
        new(name.Trim().ToLowerInvariant());
}

public sealed class AgentFamily
{
    public AgentFamilyName AgentFamilyName { get; init; }
    public required string Description { get; init; }
    public Dictionary<PolicyId, Policy> Policies { get; } = [];
}
