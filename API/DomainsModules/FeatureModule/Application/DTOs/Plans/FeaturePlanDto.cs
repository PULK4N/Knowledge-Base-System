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
    /// Base branches with nested sub-branches; the first is the main one.
    /// Empty only for plans created before plans required a base branch.
    /// </summary>
    public required IReadOnlyList<FeaturePlanBranchDto> Branches { get; init; }

    public Guid? PreviousPlanId { get; init; }

    /// <summary>
    /// IDs of the feature records this plan links, in link order.
    /// </summary>
    public required IReadOnlyCollection<Guid> RecordIds { get; init; }

    /// <summary>Requirements, definitions of done and Playwright scripts, always read with the plan.</summary>
    public required SpecificationDto Specification { get; init; }

    public IReadOnlyCollection<FeatureReviewNoteDto> ReviewNotes { get; init; } = [];

    public IReadOnlyCollection<FeatureReviewerDto> Reviewers { get; init; } = [];

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
            Branches = plan.Branches
                .Select(FeaturePlanBranchDto.FromModel)
                .ToList(),
            PreviousPlanId = plan.PreviousPlanId?.Value,
            RecordIds = plan.RecordIds
                .Select(recordId => recordId.Value)
                .ToList(),
            Specification = SpecificationDto.FromModel(plan.Specification),
            ReviewNotes = plan.ReviewNotes.Select(FeatureReviewNoteDto.FromModel).ToList(),
            Reviewers = plan.Reviewers.Select(FeatureReviewerDto.FromModel).ToList(),
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
}
