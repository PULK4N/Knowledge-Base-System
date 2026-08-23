using System.ComponentModel.DataAnnotations;

namespace ActionModule.Shared.Models;

public static class EntityQueryLimits
{
    public const int MaximumSearchLength = 500;
}

public enum SortDirection
{
    Ascending,
    Descending
}

public sealed record PageRequest(int Number, int Size)
{
    public bool IsValid => Pagination.IsValid(Number, Size);

    public int Offset => Pagination.Offset(Number, Size);
}

public sealed record SortRequest<TField>(
    TField Field,
    SortDirection Direction
) where TField : struct, Enum;

public sealed record EntityQuery<TFilter, TSort>(
    PageRequest Page,
    string? Search,
    TFilter Filters,
    SortRequest<TSort> Sort
) where TSort : struct, Enum
{
    public string? NormalizedSearch =>
        string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();

    public bool IsValid =>
        Page.IsValid
        && (Search?.Length ?? 0) <= EntityQueryLimits.MaximumSearchLength
        && Enum.IsDefined(Sort.Field)
        && Enum.IsDefined(Sort.Direction);
}

public abstract record PagedSearchRequest : IValidatableObject
{
    [Range(Pagination.DefaultPage, Pagination.MaximumPage)]
    public int Page { get; init; } = Pagination.DefaultPage;

    [Range(1, Pagination.MaximumPageSize)]
    public int PageSize { get; init; } = Pagination.DefaultPageSize;

    [StringLength(EntityQueryLimits.MaximumSearchLength)]
    public string? Search { get; init; }

    [EnumDataType(typeof(SortDirection))]
    public SortDirection SortDirection { get; init; } =
        SortDirection.Ascending;

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext
    )
    {
        if (
            Page is >= Pagination.DefaultPage and <= Pagination.MaximumPage
            && PageSize is >= 1 and <= Pagination.MaximumPageSize
            && !Pagination.IsValid(Page, PageSize)
        )
        {
            yield return new ValidationResult(
                $"The requested page exceeds the maximum offset of {Pagination.MaximumOffset} rows.",
                [nameof(Page), nameof(PageSize)]
            );
        }
    }
}
