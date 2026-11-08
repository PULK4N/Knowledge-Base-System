using System.Text.Json.Serialization;

namespace PolicyModule.Shared;

public sealed record Policy
{
    public PolicyId PolicyId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }

    /// <summary>
    /// Rebuilt from the execution timestamp during replay, without adding a
    /// field to the policy serialized inside existing event contracts.
    /// </summary>
    [JsonIgnore]
    public DateTime UpdatedAt { get; init; }
}
