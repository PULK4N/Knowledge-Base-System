namespace FeatureModule.Application.DTOs;

public sealed record FeatureReviewContextDto(
    FeatureReviewOverviewDto Feature,
    FeaturePlanDto Plan,
    string OriginBranch,
    List<FeatureReviewNoteDto> ReviewNotes,
    List<FeatureReviewerDto> Reviewers);
