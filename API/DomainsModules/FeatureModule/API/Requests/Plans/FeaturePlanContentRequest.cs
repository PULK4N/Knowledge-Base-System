using FeatureModule.Domain.Models;

namespace FeatureModule.API.Requests;

public record FeaturePlanContentRequest
{
    public required string Title { get; init; }

    public required string Content { get; init; }

    public FeaturePlanContentType ContentType { get; init; }

    public string AdditionalInfo { get; init; } = string.Empty;

    public FeaturePlanStatus Status { get; init; }
}
