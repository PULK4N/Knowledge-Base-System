using System.ComponentModel.DataAnnotations;
using ActionModule.Shared.Models;
using FeatureModule.Contracts;

namespace FeatureModule.API.Requests;

public sealed record SearchFeaturesRequest : PagedSearchRequest
{
    public Guid? ProjectId { get; init; }

    [EnumDataType(typeof(FeatureSearchSortField))]
    public FeatureSearchSortField SortBy { get; init; } =
        FeatureSearchSortField.Name;
}

public sealed record UpdateFeatureStatusRequest
{
    public required string Status { get; init; }
}

public sealed record UpdateFeatureSummaryRequest
{
    public required string Summary { get; init; }
}

public sealed record FeatureSkillRequest
{
    public required Guid SkillId { get; init; }
}

public sealed record FeatureReviewNoteContentRequest
{
    public required string Title { get; init; }

    public required string Content { get; init; }
}

public sealed record UpdateFeatureReviewNoteRequest
{
    public required Guid ReviewNoteId { get; init; }

    public required string Title { get; init; }

    public required string Content { get; init; }
}

public sealed record RemoveFeatureReviewNoteRequest
{
    public required Guid ReviewNoteId { get; init; }
}

public sealed record AddFeatureRequest
{
    public required Guid ProjectId { get; init; }

    public required string Name { get; init; }

    public required string Summary { get; init; }

    public required string Status { get; init; }
}
