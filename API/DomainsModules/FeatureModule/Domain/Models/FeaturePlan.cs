using System.Text.Json.Serialization;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeaturePlanId(Guid Value)
{
    public static FeaturePlanId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeaturePlanId FromDatabaseGuid(Guid value) =>
        new(value);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeaturePlanContentType
{
    Markdown,

    Html
}

public sealed class FeaturePlan
{
    public FeaturePlanId Id { get; init; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public FeaturePlanContentType ContentType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
