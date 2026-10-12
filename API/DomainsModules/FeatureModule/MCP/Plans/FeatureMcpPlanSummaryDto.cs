using FeatureModule.Application.DTOs;
using FeatureModule.Domain.Models;

namespace FeatureModule.MCP;

public sealed record FeatureMcpPlanSummaryDto
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required FeaturePlanStatus Status { get; init; }

    public required string AdditionalInfo { get; init; }

    /// <summary>
    /// The plan done before this one; the feature's branch tree shows where each plan branched from.
    /// </summary>
    public Guid? PreviousPlanId { get; init; }

    public static FeatureMcpPlanSummaryDto FromPlan(FeaturePlanDto plan) =>
        new()
        {
            Id = plan.Id,
            Title = plan.Title,
            Status = plan.Status,
            AdditionalInfo = plan.AdditionalInfo,
            PreviousPlanId = plan.PreviousPlanId
        };
}
