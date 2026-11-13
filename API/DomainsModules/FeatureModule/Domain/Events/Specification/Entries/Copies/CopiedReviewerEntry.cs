using FeatureModule.Domain.Models;
using ProjectModule.Shared;

namespace FeatureModule.Domain.Events;

public sealed record CopiedReviewerEntry(
    FeatureReviewerId Id,
    FeatureReviewerId OriginId,
    string Title,
    string Description,
    ReviewLevel ReviewLevel
);
