using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureReviewerDto(
    Guid Id, string Title, string Description, ReviewLevel ReviewLevel, DateTime CreatedAt, DateTime UpdatedAt,
    ItemOriginDto? Origin = null)
{
    public static FeatureReviewerDto FromModel(FeatureReviewer reviewer) =>
        new(reviewer.Id.Value, reviewer.Title, reviewer.Description, reviewer.ReviewLevel,
            reviewer.CreatedAt, reviewer.UpdatedAt, ItemOriginDto.FromModel(reviewer.Origin));
}
