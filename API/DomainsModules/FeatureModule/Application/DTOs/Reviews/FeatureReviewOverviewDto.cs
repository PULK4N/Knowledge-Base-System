namespace FeatureModule.Application.DTOs;

public sealed record FeatureReviewOverviewDto(Guid Id, Guid ProjectId, string Name, string Summary, string Status);
