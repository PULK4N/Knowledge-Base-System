using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

public sealed record CopiedReviewNoteEntry(
    FeatureReviewNoteId Id,
    FeatureReviewNoteId OriginId,
    string Title,
    string Content
);
