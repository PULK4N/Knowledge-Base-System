using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeatureReviewNoteId(Guid Value)
{
    public static FeatureReviewNoteId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeatureReviewNoteId FromDatabaseGuid(Guid value) =>
        new(value);
}

/// <summary>
/// Records a reviewed decision that intended behaviour is not a defect, so a later
/// review does not report the same finding again.
/// </summary>
public sealed class FeatureReviewNote
{
    public FeatureReviewNoteId Id { get; init; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
