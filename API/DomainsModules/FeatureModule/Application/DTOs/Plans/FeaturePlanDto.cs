using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

public sealed record FeaturePlanDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }

    public required FeaturePlanContentType ContentType { get; init; }

    public required string AdditionalInfo { get; init; }

    public required FeaturePlanStatus Status { get; init; }

    /// <summary>
    /// The base branch with nested sub-branches; null only for plans created
    /// before plans required a base branch.
    /// </summary>
    public FeaturePlanBranchDto? Branch { get; init; }

    public Guid? PreviousPlanId { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeaturePlanDto FromModel(FeaturePlan plan) =>
        new()
        {
            Id = plan.Id.Value,
            Title = plan.Title,
            Content = plan.Content,
            ContentType = plan.ContentType,
            AdditionalInfo = plan.AdditionalInfo,
            Status = plan.Status,
            Branch = plan.Branch is null
                ? null
                : FeaturePlanBranchDto.FromModel(plan.Branch),
            PreviousPlanId = plan.PreviousPlanId?.Value,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
}
