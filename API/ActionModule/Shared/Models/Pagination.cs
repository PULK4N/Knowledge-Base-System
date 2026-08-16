namespace ActionModule.Shared.Models;

public static class Pagination
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 100;
    public const int MaximumPage = int.MaxValue / MaximumPageSize;

    public static bool IsValid(int page, int pageSize) =>
        page is >= DefaultPage and <= MaximumPage
        && pageSize is >= 1 and <= MaximumPageSize
        && page <= int.MaxValue / pageSize;

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
}
