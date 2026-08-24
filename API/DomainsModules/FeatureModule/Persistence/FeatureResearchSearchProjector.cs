using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Domain.Models;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public sealed class FeatureResearchSearchProjector(
    ITextEmbeddingGenerator embeddingGenerator,
    IFeatureResearchSearchRepository repository,
    IKnowledgeSearchRepository knowledgeSearchRepository,
    IKnowledgeSearchProjectionTransaction projectionTransaction
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var featureStates = stateInfos
            .Where(stateInfo => stateInfo.StateData is FeatureStateData)
            .Select(
                stateInfo => new
                {
                    Feature = (FeatureStateData)stateInfo.StateData,
                    stateInfo.LastUpdateTimestamp
                }
            )
            .ToList();
        var features = featureStates.Select(state => state.Feature).ToList();
        var chunks = features
            .Where(feature => !feature.IsDeleted)
            .SelectMany(
                feature => FeatureResearchDiscoveryMarkdownCompiler
                    .Compile(feature)
                    .SelectMany(
                        discovery => EmbeddingTextChunker
                            .Split(discovery.Markdown)
                            .Select(
                                (text, chunkIndex) => new PendingDocument(
                                    feature.Id,
                                    feature.ProjectId,
                                    feature.Name,
                                    discovery.ResearchDiscoveryId,
                                    discovery.Title,
                                    discovery.SourceType,
                                    discovery.SourceReference,
                                    discovery.CreatedAt,
                                    discovery.UpdatedAt,
                                    chunkIndex,
                                    text
                                )
                            )
                    )
            )
            .ToList();
        var globalOnlyChunks = featureStates
            .Where(state => !state.Feature.IsDeleted)
            .SelectMany(
                state => CompileGlobalOnlyChunks(
                    state.Feature,
                    state.LastUpdateTimestamp
                )
            )
            .ToList();
        var embeddingTexts = chunks
            .Select(chunk => chunk.EmbeddingText)
            .Concat(globalOnlyChunks.Select(chunk => chunk.EmbeddingText))
            .ToList();
        var embeddings = await embeddingGenerator.Generate(
            embeddingTexts
        );
        var documents = chunks
            .Select(
                (chunk, index) => new FeatureResearchSearchDocument(
                    chunk.FeatureAggregateId,
                    chunk.FeatureName,
                    chunk.ResearchDiscoveryId,
                    chunk.Title,
                    chunk.SourceType,
                    chunk.SourceReference,
                    chunk.UpdatedAt,
                    chunk.ChunkIndex,
                    chunk.Text,
                    embeddings[index]
                )
            )
            .ToList();

        var globalDocuments = chunks
            .Select(
                (chunk, index) => ToKnowledgeDocument(
                    chunk,
                    embeddings[index]
                )
            )
            .Concat(
                globalOnlyChunks.Select(
                    (chunk, index) => new KnowledgeSearchDocument(
                        KnowledgeSearchOwnerTypes.Feature,
                        chunk.FeatureAggregateId,
                        chunk.SourceType,
                        chunk.SourceKey,
                        chunk.ChunkIndex,
                        chunk.Timestamp,
                        chunk.Metadata,
                        chunk.SearchableMetadata,
                        chunk.Text,
                        embeddings[chunks.Count + index]
                    )
                )
            )
            .ToList();
        var aggregateIds = features
            .Select(feature => feature.Id)
            .Distinct()
            .ToList();
        await projectionTransaction.Execute(
            async () =>
            {
                await repository.Write(aggregateIds, documents);
                await knowledgeSearchRepository.Write(
                    KnowledgeSearchOwnerTypes.Feature,
                    aggregateIds,
                    globalDocuments
                );
            }
        );
    }

    private static KnowledgeSearchDocument ToKnowledgeDocument(
        PendingDocument chunk,
        System.Collections.Immutable.ImmutableArray<float> embedding
    ) =>
        new(
            KnowledgeSearchOwnerTypes.Feature,
            chunk.FeatureAggregateId,
            KnowledgeSearchSourceTypes.FeatureResearchDiscovery,
            chunk.ResearchDiscoveryId.ToString("N"),
            chunk.ChunkIndex,
            chunk.UpdatedAt,
            KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
            {
                ["featureId"] = chunk.FeatureAggregateId.Value.ToString(),
                ["projectId"] = chunk.ProjectId.Value.ToString(),
                ["featureName"] = chunk.FeatureName,
                ["researchDiscoveryId"] = chunk.ResearchDiscoveryId.ToString(),
                ["title"] = chunk.Title,
                ["sourceType"] = chunk.SourceType,
                ["sourceReference"] = chunk.SourceReference,
                ["createdAt"] = chunk.CreatedAt,
                ["updatedAt"] = chunk.UpdatedAt
            }),
            $"{chunk.FeatureName} {chunk.Title} {chunk.SourceType} {chunk.SourceReference}",
            chunk.Text,
            embedding
        );

    private static List<PendingGlobalDocument> CompileGlobalOnlyChunks(
        FeatureStateData feature,
        DateTime lastUpdateTimestamp
    )
    {
        var chunks = new List<PendingGlobalDocument>();
        AddChunks(
            chunks,
            feature,
            KnowledgeSearchSourceTypes.Feature,
            "overview",
            lastUpdateTimestamp,
            KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
            {
                ["featureId"] = feature.Id.Value.ToString(),
                ["featureName"] = feature.Name,
                ["projectId"] = feature.ProjectId.Value.ToString(),
                ["relatedSkillIds"] = feature.RelatedSkillIds
                    .Select(id => id.Value)
                    .ToList(),
                ["updatedAt"] = lastUpdateTimestamp
            }),
            $"# {feature.Name}\n\n## Summary\n\n{feature.Summary}\n\n"
                + $"## Status\n\n{feature.Status}"
        );

        foreach (var plan in feature.Plans)
        {
            AddChunks(
                chunks,
                feature,
                KnowledgeSearchSourceTypes.FeaturePlan,
                plan.Id.Value.ToString("N"),
                plan.UpdatedAt,
                KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
                {
                    ["featureId"] = feature.Id.Value.ToString(),
                    ["projectId"] = feature.ProjectId.Value.ToString(),
                    ["featureName"] = feature.Name,
                    ["planId"] = plan.Id.Value.ToString(),
                    ["title"] = plan.Title,
                    ["contentType"] = plan.ContentType.ToString(),
                    ["isCurrent"] = feature.CurrentPlanId == plan.Id,
                    ["createdAt"] = plan.CreatedAt,
                    ["updatedAt"] = plan.UpdatedAt
                }),
                $"# {plan.Title}\n\n{plan.Content}"
            );
        }

        foreach (var record in feature.Records)
        {
            AddChunks(
                chunks,
                feature,
                KnowledgeSearchSourceTypes.FeatureRecord,
                record.Id.Value.ToString("N"),
                record.UpdatedAt,
                KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
                {
                    ["featureId"] = feature.Id.Value.ToString(),
                    ["projectId"] = feature.ProjectId.Value.ToString(),
                    ["featureName"] = feature.Name,
                    ["recordId"] = record.Id.Value.ToString(),
                    ["createdAt"] = record.CreatedAt,
                    ["updatedAt"] = record.UpdatedAt
                }),
                $"# Feature conversation record\n\n## User\n\n{record.UserMessage}"
                    + $"\n\n## AI\n\n{record.AiAnswer}"
            );
        }

        return chunks;
    }

    private static void AddChunks(
        List<PendingGlobalDocument> chunks,
        FeatureStateData feature,
        string sourceType,
        string sourceKey,
        DateTime? timestamp,
        System.Text.Json.JsonElement metadata,
        string markdown
    )
    {
        chunks.AddRange(
            EmbeddingTextChunker.Split(markdown).Select(
                (text, chunkIndex) => new PendingGlobalDocument(
                    feature.Id,
                    feature.Name,
                    sourceType,
                    sourceKey,
                    chunkIndex,
                    timestamp,
                    metadata,
                    $"{feature.Name} {sourceType}",
                    text
                )
            )
        );
    }

    private sealed record PendingDocument(
        AggregateId FeatureAggregateId,
        AggregateId ProjectId,
        string FeatureName,
        Guid ResearchDiscoveryId,
        string Title,
        string SourceType,
        string SourceReference,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        int ChunkIndex,
        string Text
    )
    {
        public string EmbeddingText =>
            $"Feature: {FeatureName}\nResearch discovery: {Title}\n"
            + $"Source type: {SourceType}\nSource: {SourceReference}\n\n{Text}";
    }

    private sealed record PendingGlobalDocument(
        AggregateId FeatureAggregateId,
        string FeatureName,
        string SourceType,
        string SourceKey,
        int ChunkIndex,
        DateTime? Timestamp,
        System.Text.Json.JsonElement Metadata,
        string SearchableMetadata,
        string Text
    )
    {
        public string EmbeddingText =>
            $"Feature: {FeatureName}\nType: {SourceType}\n\n{Text}";
    }
}
