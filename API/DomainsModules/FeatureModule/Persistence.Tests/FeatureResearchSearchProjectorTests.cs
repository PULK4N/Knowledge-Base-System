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
    public async Task Update_reuses_research_embeddings_and_adds_all_feature_sources_to_global_search()
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
            Name = "Hybrid feature search",
            ProjectId = AggregateId.FromDatabaseGuid(
                Guid.Parse("33333333-3333-3333-3333-333333333333")
            )
        };
        var createdAt = updatedAt.AddDays(-1);
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
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        );
        var planId = FeaturePlanId.FromDatabaseGuid(Guid.NewGuid());
        state.Plans.Add(
            new FeaturePlan
            {
                Id = planId,
                Title = "Implementation",
                Content = "# Build the global projection",
                ContentType = FeaturePlanContentType.Markdown,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        );
        state.CurrentPlanId = planId;
        state.Records.Add(
            new FeatureRecord
            {
                Id = FeatureRecordId.FromDatabaseGuid(Guid.NewGuid()),
                UserMessage = "DO-NOT-EMBED-RECORD",
                AiAnswer = "Included only in global search",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        );
        var generator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var projector = new FeatureResearchSearchProjector(
            generator,
            repository,
            knowledgeRepository,
            new ImmediateTransaction()
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
        Assert.Equal(1, generator.CallCount);
        Assert.Contains(
            generator.LastInputs,
            input => input.Contains("Research discovery: PostgreSQL ranking")
        );
        Assert.Equal(
            [
                KnowledgeSearchSourceTypes.Feature,
                KnowledgeSearchSourceTypes.FeaturePlan,
                KnowledgeSearchSourceTypes.FeatureRecord,
                KnowledgeSearchSourceTypes.FeatureResearchDiscovery
            ],
            knowledgeRepository.Documents
                .Select(document => document.SourceType)
                .Distinct()
                .OrderBy(sourceType => sourceType)
                .ToList()
        );
        var globalResearch = knowledgeRepository.Documents.First(
            document => document.SourceType
                == KnowledgeSearchSourceTypes.FeatureResearchDiscovery
        );
        Assert.Equal(
            createdAt,
            globalResearch.Metadata.GetProperty("createdAt").GetDateTime()
        );
        Assert.Equal(
            updatedAt,
            globalResearch.Metadata.GetProperty("updatedAt").GetDateTime()
        );
        Assert.Equal(
            state.ProjectId.Value.ToString(),
            globalResearch.Metadata.GetProperty("projectId").GetString()
        );
        Assert.Contains(
            generator.LastInputs,
            input => input.Contains("DO-NOT-EMBED-RECORD")
        );
        Assert.Contains(
            knowledgeRepository.Documents,
            document => document.SourceType
                == KnowledgeSearchSourceTypes.FeatureResearchDiscovery
                && document.Embedding == repository.Documents[0].Embedding
        );
        Assert.Contains(
            knowledgeRepository.Documents,
            document => document.SourceType
                == KnowledgeSearchSourceTypes.FeatureRecord
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
        var knowledgeRepository = new FakeKnowledgeRepository();
        var projector = new FeatureResearchSearchProjector(
            new FakeEmbeddingGenerator(),
            repository,
            knowledgeRepository,
            new ImmediateTransaction()
        );

        await projector.Update(
            [StateInfo.Create(state, "features-state-machine", FeatureId)]
        );

        Assert.Equal([FeatureId], repository.FeatureIds);
        Assert.Empty(repository.Documents);
        Assert.Equal([FeatureId], knowledgeRepository.OwnerAggregateIds);
        Assert.Empty(knowledgeRepository.Documents);
    }

    private sealed class FakeEmbeddingGenerator : ITextEmbeddingGenerator
    {
        public IReadOnlyList<string> LastInputs { get; private set; } = [];
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<ImmutableArray<float>>> Generate(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default
        )
        {
            CallCount++;
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

    private sealed class FakeKnowledgeRepository : IKnowledgeSearchRepository
    {
        public List<KnowledgeSearchDocument> Documents { get; private set; } = [];
        public List<AggregateId> OwnerAggregateIds { get; private set; } = [];

        public Task Write(string ownerType, List<AggregateId> ownerAggregateIds, List<KnowledgeSearchDocument> documents, CancellationToken cancellationToken = default)
        {
            OwnerAggregateIds = ownerAggregateIds;
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<List<KnowledgeSearchCandidate>> SearchText(string query, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<KnowledgeSearchCandidate>> SearchVector(ImmutableArray<float> embedding, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ImmediateTransaction : IKnowledgeSearchProjectionTransaction
    {
        public Task Execute(Func<Task> writes, CancellationToken cancellationToken = default) => writes();
    }
}
