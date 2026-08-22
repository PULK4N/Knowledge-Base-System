using FeatureModule.Domain.Models;

namespace FeatureModule.API.Requests;

public sealed record UpdateFeatureStatusRequest
{
    public required string Status { get; init; }
}

public sealed record FeatureSkillRequest
{
    public required Guid SkillId { get; init; }
}

public sealed record FeatureRecordContentRequest
{
    public required string UserMessage { get; init; }

    public required string AiAnswer { get; init; }
}

public sealed record UpdateFeatureRecordRequest
{
    public required Guid RecordId { get; init; }

    public required string UserMessage { get; init; }

    public required string AiAnswer { get; init; }
}

public sealed record RemoveFeatureRecordRequest
{
    public required Guid RecordId { get; init; }
}

public sealed record FeatureResearchDiscoveryContentRequest
{
    public required string Content { get; init; }

    public FeatureResearchDiscoverySourceType SourceType { get; init; }

    public string SourceReference { get; init; } = string.Empty;
}

public sealed record UpdateFeatureResearchDiscoveryRequest
{
    public required Guid DiscoveryId { get; init; }

    public required string Content { get; init; }

    public FeatureResearchDiscoverySourceType SourceType { get; init; }

    public string SourceReference { get; init; } = string.Empty;
}

public sealed record RemoveFeatureResearchDiscoveryRequest
{
    public required Guid DiscoveryId { get; init; }
}

public sealed record FeaturePlanContentRequest
{
    public required string Title { get; init; }

    public required string Content { get; init; }

    public FeaturePlanContentType ContentType { get; init; }
}

public sealed record FeaturePlanRequest
{
    public required Guid PlanId { get; init; }
}
