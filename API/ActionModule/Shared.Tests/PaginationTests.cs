using ActionModule.Shared.Models;

namespace ActionModule.Shared.Tests;

public sealed class PaginationTests
{
    [Fact]
    public void PagedResult_calculates_navigation_metadata()
    {
        var result = new PagedResult<int>([1, 2], 2, 2, 5);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void Pagination_rejects_invalid_and_overflowing_pages()
    {
        Assert.False(Pagination.IsValid(0, Pagination.DefaultPageSize));
        Assert.False(Pagination.IsValid(1, Pagination.MaximumPageSize + 1));
        Assert.False(Pagination.IsValid(int.MaxValue, 2));
        Assert.True(
            Pagination.IsValid(
                Pagination.DefaultPage,
                Pagination.DefaultPageSize
            )
        );
    }
}
