using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence.Tests;

public sealed class FeatureResearchSearchProjectorTests
{
    private static readonly AggregateId FeatureId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public async Task Update_projects_research_discoveries_only()
    {
        var discoveryId = Guid.Parse(
            "22222222-2222-2222-2222-222222222222"
        );
        var updatedAt = new DateTime(
            2026,
            8,
            24,
            20,
            0,
            0,
            DateTimeKind.Utc
        );
        var state = new FeatureStateData(FeatureId)
        {
            Name = "Hybrid feature search"
        };
        state.ResearchDiscoveries.Add(
            new FeatureResearchDiscovery
            {
                Id = FeatureResearchDiscoveryId.FromDatabaseGuid(
                    discoveryId
                ),
                Title = "PostgreSQL ranking",
                Content = "# Finding\nUse reciprocal-rank fusion.",
                SourceType = FeatureResearchDiscoverySourceType.Code,
                SourceReference = "API/PostgreSqlModule",
                UpdatedAt = updatedAt
            }
        );
        state.Records.Add(
            new FeatureRecord
            {
                Id = FeatureRecordId.FromDatabaseGuid(Guid.NewGuid()),
                UserMessage = "DO-NOT-EMBED-RECORD",
                AiAnswer = "Also excluded"
            }
        );
        var generator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository();
        var projector = new FeatureResearchSearchProjector(
            generator,
            repository
        );

        await projector.Update(
            [StateInfo.Create(state, "features-state-machine", FeatureId)]
        );

        Assert.Equal([FeatureId], repository.FeatureIds);
        Assert.NotEmpty(repository.Documents);
        Assert.All(
            repository.Documents,
            document =>
            {
                Assert.Equal(discoveryId, document.ResearchDiscoveryId);
                Assert.Equal("Hybrid feature search", document.FeatureName);
                Assert.Equal("PostgreSQL ranking", document.Title);
                Assert.Equal("Code", document.SourceType);
                Assert.Equal("API/PostgreSqlModule", document.SourceReference);
                Assert.Equal(updatedAt, document.UpdatedAt);
                Assert.Equal([1f, 2f], document.Embedding.ToArray());
                Assert.DoesNotContain("DO-NOT-EMBED-RECORD", document.Text);
            }
        );
        Assert.All(
            generator.LastInputs,
            input =>
            {
                Assert.Contains("Feature: Hybrid feature search", input);
                Assert.Contains("Research discovery: PostgreSQL ranking", input);
                Assert.DoesNotContain("DO-NOT-EMBED-RECORD", input);
            }
        );
    }

    [Fact]
    public async Task Update_replaces_deleted_feature_with_empty_snapshot()
    {
        var state = new FeatureStateData(FeatureId)
        {
            Name = "Deleted feature",
            IsDeleted = true
        };
        var repository = new FakeRepository();
        var projector = new FeatureResearchSearchProjector(
            new FakeEmbeddingGenerator(),
            repository
        );

        await projector.Update(
            [StateInfo.Create(state, "features-state-machine", FeatureId)]
        );

        Assert.Equal([FeatureId], repository.FeatureIds);
        Assert.Empty(repository.Documents);
    }

    private sealed class FakeEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public IReadOnlyList<string> LastInputs { get; private set; } = [];

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            LastInputs = inputs;
            return Task.FromResult<IReadOnlyList<ImmutableArray<float>>>(
                inputs.Select(_ => ImmutableArray.Create(1f, 2f)).ToList()
            );
        }
    }

    private sealed class FakeRepository : IFeatureResearchSearchRepository
    {
        public List<AggregateId> FeatureIds { get; private set; } = [];
        public List<FeatureResearchSearchDocument> Documents { get; private set; } = [];

        public Task Write(
            List<AggregateId> featureAggregateIds,
            List<FeatureResearchSearchDocument> documents,
            CancellationToken cancellationToken = default
        )
        {
            FeatureIds = featureAggregateIds;
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<List<FeatureResearchSearchCandidate>> SearchText(
            string query,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<FeatureResearchSearchCandidate>> SearchVector(
            ImmutableArray<float> embedding,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}
