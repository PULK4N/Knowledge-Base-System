using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Domain;
using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Persistence;

public sealed class FeatureResearchSearchProjector(
    ITextEmbeddingGenerator embeddingGenerator,
    IFeatureResearchSearchRepository repository
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var features = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<FeatureStateData>()
            .ToList();
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
                                    feature.Name,
                                    discovery.ResearchDiscoveryId,
                                    discovery.Title,
                                    discovery.SourceType,
                                    discovery.SourceReference,
                                    discovery.UpdatedAt,
                                    chunkIndex,
                                    text
                                )
                            )
                    )
            )
            .ToList();
        var embeddings = await embeddingGenerator.Generate(
            chunks.Select(chunk => chunk.EmbeddingText).ToList()
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

        await repository.Write(
            features.Select(feature => feature.Id).Distinct().ToList(),
            documents
        );
    }

    private sealed record PendingDocument(
        AggregateId FeatureAggregateId,
        string FeatureName,
        Guid ResearchDiscoveryId,
        string Title,
        string SourceType,
        string SourceReference,
        DateTime UpdatedAt,
        int ChunkIndex,
        string Text
    )
    {
        public string EmbeddingText =>
            $"Feature: {FeatureName}\nResearch discovery: {Title}\n"
            + $"Source type: {SourceType}\nSource: {SourceReference}\n\n{Text}";
    }
}
