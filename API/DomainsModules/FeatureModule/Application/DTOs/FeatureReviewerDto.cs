using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureReviewerDto(
    Guid Id, string Title, string Description, ReviewLevel ReviewLevel, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static FeatureReviewerDto FromModel(FeatureReviewer reviewer) =>
        new(reviewer.Id.Value, reviewer.Title, reviewer.Description, reviewer.ReviewLevel,
            reviewer.CreatedAt, reviewer.UpdatedAt);
}
