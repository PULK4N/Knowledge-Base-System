using FeatureModule.Application.DTOs;

namespace FeatureModule.MCP;

public sealed record FeatureMcpDto
{
    public required Guid Id { get; init; }

    public required Guid ProjectId { get; init; }

    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required string Status { get; init; }

    public required IReadOnlyCollection<Guid> RelatedSkillIds { get; init; }

    /// <summary>
    /// The feature branch and the branches of plans not yet completed, one per
    /// line with the branches each came from.
    /// </summary>
    public string? BranchGraph { get; init; }

    /// <summary>
    /// Plan summaries without content; load a plan with feature_plan_get when needed.
    /// </summary>
    public required IReadOnlyCollection<FeatureMcpPlanSummaryDto>
        Plans { get; init; }

    /// <summary>
    /// Every record without content, newest first; load records with feature_record_get.
    /// </summary>
    public required IReadOnlyCollection<FeatureRecordReferenceDto>
        Records { get; init; }

    public static FeatureMcpDto FromFeature(FeatureDto feature) =>
        new()
        {
            Id = feature.Id,
            ProjectId = feature.ProjectId,
            Name = feature.Name,
            Summary = feature.Summary,
            Status = feature.Status,
            RelatedSkillIds = feature.RelatedSkillIds,
            BranchGraph = FeatureBranchGraphFormatter.Format(feature),
            Plans = feature.Plans
                .OrderByDescending(plan => plan.UpdatedAt)
                .ThenBy(plan => plan.Id)
                .Select(FeatureMcpPlanSummaryDto.FromPlan)
                .ToList(),
            Records = feature.Records
                .OrderByDescending(record => record.UpdatedAt)
                .ThenBy(record => record.Id)
                .Select(FeatureRecordReferenceDto.FromDto)
                .ToList()
        };
}
