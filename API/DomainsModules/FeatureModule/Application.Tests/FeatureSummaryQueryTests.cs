using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using FeatureModule.Application.DTOs;
using FeatureModule.Application.Queries;
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
}
