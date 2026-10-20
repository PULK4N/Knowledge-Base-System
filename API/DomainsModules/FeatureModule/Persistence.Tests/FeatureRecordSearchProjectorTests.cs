using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence.Tests;

public sealed class FeatureRecordSearchProjectorTests
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
        state.Records.Add(
            new FeatureRecord
            {
                Id = FeatureRecordId.FromDatabaseGuid(discoveryId),
                Type = FeatureRecordTypes.Research,
                Title = "PostgreSQL ranking",
                Content = "# Finding\nUse reciprocal-rank fusion.",
                SourceType = FeatureRecordSourceType.Code,
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
                AdditionalInfo = "Production implementation.",
                Status = FeaturePlanStatus.InProgress,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        );
        var conversationId = Guid.NewGuid();
        state.Records.Add(
            new FeatureRecord
            {
                Id = FeatureRecordId.FromDatabaseGuid(conversationId),
                Type = FeatureRecordTypes.Conversation,
                Title = "Why rank by fusion?",
                Content = "## User\n\nWhy?\n\n## AI\n\nIt mixes both legs.",
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            }
        );
        var generator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var projector = new FeatureRecordSearchProjector(
            generator,
            new FakeProjectionWriter(repository, knowledgeRepository)
        );

        await projector.Update(
            [StateInfo.Create(state, "features-state-machine", FeatureId)]
        );

        Assert.Equal([FeatureId], repository.FeatureIds);
        Assert.Equal(
            [discoveryId, conversationId],
            repository.Documents
                .Select(document => document.RecordId)
                .Distinct()
                .ToList()
        );
        var research = repository.Documents.First(
            document => document.RecordId == discoveryId
        );
        Assert.Equal(FeatureRecordTypes.Research, research.RecordType);
        Assert.Equal("Hybrid feature search", research.FeatureName);
        Assert.Equal("PostgreSQL ranking", research.Title);
        Assert.Equal("Code", research.SourceType);
        Assert.Equal("API/PostgreSqlModule", research.SourceReference);
        Assert.Equal(updatedAt, research.UpdatedAt);
        Assert.Equal([1f, 2f], research.Embedding.ToArray());
        var conversation = repository.Documents.First(
            document => document.RecordId == conversationId
        );
        Assert.Equal(FeatureRecordTypes.Conversation, conversation.RecordType);
        Assert.Equal(string.Empty, conversation.SourceType);
        Assert.Equal(1, generator.CallCount);
        Assert.Contains(
            generator.LastInputs,
            input => input.Contains("Record type: research\nRecord: PostgreSQL ranking")
        );
        Assert.Equal(
            [
                KnowledgeSearchSourceTypes.Feature,
                KnowledgeSearchSourceTypes.FeaturePlan,
                KnowledgeSearchSourceTypes.FeatureRecord
            ],
            knowledgeRepository.Documents
                .Select(document => document.SourceType)
                .Distinct()
                .OrderBy(sourceType => sourceType)
                .ToList()
        );
        var globalPlan = knowledgeRepository.Documents.First(
            document => document.SourceType
                == KnowledgeSearchSourceTypes.FeaturePlan
        );
        Assert.Equal(
            "InProgress",
            globalPlan.Metadata.GetProperty("status").GetString()
        );
        Assert.Equal(
            "Production implementation.",
            globalPlan.Metadata.GetProperty("additionalInfo").GetString()
        );
        Assert.False(globalPlan.Metadata.TryGetProperty("isCurrent", out _));
        Assert.Contains("Production implementation.", globalPlan.Text);
        var globalResearch = knowledgeRepository.Documents.First(
            document => document.SourceType
                == KnowledgeSearchSourceTypes.FeatureRecord
                && document.SourceKey == discoveryId.ToString("N")
        );
        Assert.Equal(
            FeatureRecordTypes.Research,
            globalResearch.Metadata.GetProperty("recordType").GetString()
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
        Assert.Equal(research.Embedding, globalResearch.Embedding);
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
        var projector = new FeatureRecordSearchProjector(
            new FakeEmbeddingGenerator(),
            new FakeProjectionWriter(repository, knowledgeRepository)
        );

        await projector.Update(
            [StateInfo.Create(state, "features-state-machine", FeatureId)]
        );

        Assert.Equal([FeatureId], repository.FeatureIds);
        Assert.Empty(repository.Documents);
        Assert.Equal([FeatureId], knowledgeRepository.OwnerAggregateIds);
        Assert.Empty(knowledgeRepository.Documents);
    }

    private sealed class FakeEmbeddingGenerator : IProjectionEmbeddingGenerator
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

    private sealed class FakeRepository : IFeatureRecordSearchRepository
    {
        public List<AggregateId> FeatureIds { get; private set; } = [];
        public List<FeatureRecordSearchDocument> Documents { get; private set; } = [];

        public Task Write(
            List<AggregateId> featureAggregateIds,
            List<FeatureRecordSearchDocument> documents,
            CancellationToken cancellationToken = default
        )
        {
            FeatureIds = featureAggregateIds;
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<List<FeatureRecordSearchCandidate>> SearchText(
            List<string> keywords,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<FeatureRecordSearchCandidate>> SearchVector(
            ImmutableArray<float> embedding,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<FeatureRecordSearchCandidate>> SearchTextBySource(
            List<string> keywords,
            int sourceCount,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<List<FeatureRecordSearchCandidate>> SearchVectorBySource(
            ImmutableArray<float> embedding,
            int sourceCount,
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

        public Task<List<KnowledgeSearchCandidate>> SearchText(List<string> keywords, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<KnowledgeSearchCandidate>> SearchVector(ImmutableArray<float> embedding, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<KnowledgeSearchCandidate>> SearchTextBySource(List<string> keywords, int sourceCount, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<List<KnowledgeSearchCandidate>> SearchVectorBySource(ImmutableArray<float> embedding, int sourceCount, int candidateCount, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FakeProjectionWriter(
        FakeRepository repository,
        FakeKnowledgeRepository knowledgeRepository
    ) : IFeatureSearchProjectionWriter
    {
        public async Task Write(
            FeatureSearchProjectionBatch batch,
            CancellationToken cancellationToken = default
        )
        {
            await repository.Write(
                batch.FeatureAggregateIds,
                batch.RecordDocuments,
                cancellationToken
            );
            await knowledgeRepository.Write(
                KnowledgeSearchOwnerTypes.Feature,
                batch.FeatureAggregateIds,
                batch.KnowledgeDocuments,
                cancellationToken
            );
        }
    }
}
