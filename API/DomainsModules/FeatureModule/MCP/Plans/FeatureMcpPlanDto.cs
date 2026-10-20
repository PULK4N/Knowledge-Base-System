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
    /// Each base branch with its sub-branches rendered as a tree, each base line
    /// followed by the branches it came from; null only for plans created before
    /// plans required a base branch.
    /// </summary>
    public string? Branches { get; init; }

    public Guid? PreviousPlanId { get; init; }

    /// <summary>
    /// Linked records without content; load them with feature_record_get.
    /// </summary>
    public required IReadOnlyCollection<FeatureRecordReferenceDto>
        Records { get; init; }

    public required FeaturePlanContentType ContentType { get; init; }

    public required string Content { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public static FeatureMcpPlanDto FromPlan(FeaturePlanDto plan, FeatureDto feature)
    {
        var recordsById = feature.Records.ToDictionary(record => record.Id);

        return new FeatureMcpPlanDto
        {
            Id = plan.Id,
            Title = plan.Title,
            Status = plan.Status,
            AdditionalInfo = plan.AdditionalInfo,
            Branches = FeaturePlanBranchTreeFormatter.FormatBaseBranches(plan, feature),
            PreviousPlanId = plan.PreviousPlanId,
            Records = plan.RecordIds
                .Where(recordsById.ContainsKey)
                .Select(recordId => FeatureRecordReferenceDto.FromDto(
                    recordsById[recordId]
                ))
                .ToList(),
            ContentType = plan.ContentType,
            Content = plan.Content,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }
}
