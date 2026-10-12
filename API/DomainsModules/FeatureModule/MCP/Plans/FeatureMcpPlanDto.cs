using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP;

public sealed record FeatureMcpPlanDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required FeaturePlanStatus Status { get; init; }

    public required string AdditionalInfo { get; init; }

    /// <summary>
    /// Base branch and sub-branches rendered as a tree; null only for plans created before plans required a base branch.
    /// </summary>
    public string? Branch { get; init; }

    public required FeaturePlanContentType ContentType { get; init; }

    public required string Content { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeatureMcpPlanDto FromPlan(FeaturePlanDto plan) =>
        new()
        {
            Id = plan.Id,
            Title = plan.Title,
            Status = plan.Status,
            AdditionalInfo = plan.AdditionalInfo,
            Branch = FeaturePlanBranchTreeFormatter.Format(plan.Branch),
            ContentType = plan.ContentType,
            Content = plan.Content,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
}
