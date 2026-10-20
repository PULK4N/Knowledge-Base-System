using FeatureModule.Domain.Models;

namespace FeatureModule.API.Requests;

public record FeatureRecordContentRequest
{
    public required string Type { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public FeatureRecordSourceType? SourceType { get; init; }

    public string SourceReference { get; init; } = string.Empty;
}
