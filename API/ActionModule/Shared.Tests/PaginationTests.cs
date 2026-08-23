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
    public void PagedResult_maps_items_and_preserves_metadata()
    {
        var result = new PagedResult<int>([1, 2], 2, 2, 5);

        var mapped = result.Map(value => value.ToString());

        Assert.Equal(["1", "2"], mapped.Items);
        Assert.Equal(result.Page, mapped.Page);
        Assert.Equal(result.PageSize, mapped.PageSize);
        Assert.Equal(result.TotalCount, mapped.TotalCount);
    }

    [Fact]
    public void Pagination_rejects_invalid_and_overflowing_pages()
    {
        Assert.False(Pagination.IsValid(0, Pagination.DefaultPageSize));
        Assert.False(Pagination.IsValid(1, Pagination.MaximumPageSize + 1));
        Assert.False(Pagination.IsValid(Pagination.MaximumPage, 2));
        Assert.True(Pagination.IsValid(Pagination.MaximumPage, 1));
        Assert.True(
            Pagination.IsValid(
                Pagination.DefaultPage,
                Pagination.DefaultPageSize
            )
        );
    }

    [Theory]
    [InlineData(1, 25, true)]
    [InlineData(0, 25, false)]
    [InlineData(1, 101, false)]
    [InlineData(100_001, 2, false)]
    public void PageRequest_uses_shared_pagination_validation(
        int page,
        int pageSize,
        bool expected
    )
    {
        var request = new PageRequest(page, pageSize);

        Assert.Equal(expected, request.IsValid);
    }

    [Fact]
    public void EntityQuery_rejects_oversized_search_and_undefined_enums()
    {
        var valid = new EntityQuery<TestFilters, TestSortField>(
            new PageRequest(1, 25),
            new string('a', EntityQueryLimits.MaximumSearchLength),
            new TestFilters(),
            new SortRequest<TestSortField>(
                TestSortField.Name,
                SortDirection.Ascending
            )
        );

        Assert.True(valid.IsValid);
        Assert.False(
            (valid with
            {
                Search = new string(
                    'a',
                    EntityQueryLimits.MaximumSearchLength + 1
                )
            }).IsValid
        );
        Assert.False(
            (valid with
            {
                Sort = new SortRequest<TestSortField>(
                    (TestSortField)999,
                    SortDirection.Ascending
                )
            }).IsValid
        );
    }

    private sealed record TestFilters;

    private enum TestSortField
    {
        Name
    }
}
