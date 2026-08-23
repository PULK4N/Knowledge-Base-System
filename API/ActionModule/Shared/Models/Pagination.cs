namespace ActionModule.Shared.Models;

public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;
    public const int MaximumOffset = 100_000;
    public const int MaximumPage = MaximumOffset + DefaultPage;

    public static bool IsValid(int page, int pageSize) =>
        page is >= DefaultPage and <= MaximumPage
        && pageSize is >= 1 and <= MaximumPageSize
        && (long)(page - DefaultPage) * pageSize <= MaximumOffset;

    public static int Offset(int page, int pageSize) =>
        (page - 1) * pageSize;
}

public sealed record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount
)
{
    public int TotalPages =>
        TotalCount == 0
            ? 0
            : TotalCount / PageSize
                + (TotalCount % PageSize == 0 ? 0 : 1);

    public bool HasPreviousPage => Page > Pagination.DefaultPage;
    public bool HasNextPage => Page < TotalPages;

    public PagedResult<TResult> Map<TResult>(Func<T, TResult> map) =>
        new(
            Items.Select(map).ToList(),
            Page,
            PageSize,
            TotalCount
        );
}
