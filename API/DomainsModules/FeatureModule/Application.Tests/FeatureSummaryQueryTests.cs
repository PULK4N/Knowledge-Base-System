using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Queries;
using FeatureModule.Contracts;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.Tests;

public sealed class FeatureSummaryQueryTests
{
    [Fact]
    public async Task List_and_get_by_name_map_feature_summaries()
    {
        var featureId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var projectId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var repository = new StubFeatureSummaryRepository(
            [
                new FeatureSummary(
                    featureId,
                    projectId,
                    "Pagination",
                    "Study paging patterns.",
                    "Planning",
                    null,
                    0,
                    0
                )
            ]
        );
        var executor = new Executor
        {
            Id = EventExecutor.FromDatabaseGuid(
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            )
        };

        var features = await new ListFeaturesQuery(repository)
            .Execute(executor);
        var feature = await new GetFeatureByNameQuery(repository)
        {
            Name = "  PAGINATION "
        }.Execute(executor);

        var expected = new FeatureSummaryDto(
            featureId,
            projectId,
            "Pagination",
            "Study paging patterns.",
            "Planning",
            null,
            0,
            0
        );
        Assert.Equal(expected, Assert.Single(features));
        Assert.Equal(expected, feature);
    }

    [Fact]
    public async Task Search_maps_shared_query_criteria_and_page_result()
    {
        var featureId = Guid.Parse(
            "11111111-1111-1111-1111-111111111111"
        );
        var projectId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var repository = new StubFeatureSearchRepository(
            new PagedResult<FeatureSummary>(
                [
                    new FeatureSummary(
                        featureId,
                        projectId,
                        "Pagination",
                        "Study paging patterns.",
                        "Planning",
                        null,
                        2,
                        3
                    )
                ],
                2,
                5,
                7
            )
        );
        var query = new SearchFeaturesQuery(repository)
        {
            Page = 2,
            PageSize = 5,
            Search = " paging ",
            ProjectId = projectId,
            SortBy = FeatureSearchSortField.PlanCount,
            SortDirection = SortDirection.Descending
        };

        var result = await query.Execute(
            new Executor { Id = EventExecutor.New() }
        );

        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(featureId, Assert.Single(result.Items).FeatureId);
        var request = Assert.IsType<
            EntityQuery<FeatureSearchFilters, FeatureSearchSortField>
        >(repository.LastRequest);
        Assert.Equal(new PageRequest(2, 5), request.Page);
        Assert.Equal("paging", request.NormalizedSearch);
        Assert.Equal(
            new FeatureSearchFilters(projectId),
            request.Filters
        );
        Assert.Equal(FeatureSearchSortField.PlanCount, request.Sort.Field);
        Assert.Equal(SortDirection.Descending, request.Sort.Direction);
    }

    [Fact]
    public async Task Search_rejects_invalid_non_http_criteria()
    {
        var repository = new StubFeatureSearchRepository(
            new PagedResult<FeatureSummary>([], 1, 25, 0)
        );
        var executor = new Executor { Id = EventExecutor.New() };

        var oversizedSearch = new SearchFeaturesQuery(repository)
        {
            Search = new string(
                'a',
                EntityQueryLimits.MaximumSearchLength + 1
            )
        };
        var undefinedSort = new SearchFeaturesQuery(repository)
        {
            SortBy = (FeatureSearchSortField)999
        };

        Assert.False(await oversizedSearch.CanExecute(executor));
        Assert.False(await undefinedSort.CanExecute(executor));
    }

    private sealed class StubFeatureSummaryRepository(
        List<FeatureSummary> features
    ) : IFeatureSummaryRepository
    {
        public Task<List<FeatureSummary>> List(
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(features);

        public Task<FeatureSummary?> GetByName(
            string name,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                features.SingleOrDefault(
                    feature => string.Equals(
                        feature.Name,
                        name.Trim(),
                        StringComparison.OrdinalIgnoreCase
                    )
                )
            );

        public Task<FeatureSummarySearchResult> Search(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken = default
        ) =>
            Task.FromResult(
                new FeatureSummarySearchResult(features, features.Count)
            );
    }

    private sealed class StubFeatureSearchRepository(
        PagedResult<FeatureSummary> result
    ) : IFeatureSearchRepository
    {
        public object? LastRequest { get; private set; }

        public Task<PagedResult<FeatureSummary>> Search(
            EntityQuery<FeatureSearchFilters, FeatureSearchSortField> request,
            CancellationToken cancellationToken = default
        )
        {
            LastRequest = request;
            return Task.FromResult(result);
        }
    }
}
