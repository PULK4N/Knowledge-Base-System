using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence;

public sealed class SkillSearchProjector(
    ITextEmbeddingGenerator embeddingGenerator,
    ISkillSearchProjectionWriter projectionWriter
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var skillStates = stateInfos
            .Where(stateInfo => stateInfo.StateData is SkillStateData)
            .Select(
                stateInfo => new
                {
                    Skill = (SkillStateData)stateInfo.StateData,
                    stateInfo.LastUpdateTimestamp
                }
            )
            .ToList();
        var skills = skillStates.Select(state => state.Skill).ToList();
        var lastUpdates = skillStates.ToDictionary(
            state => state.Skill.Id,
            state => state.LastUpdateTimestamp
        );
        var chunks = skills
            .Where(skill => !skill.IsDeleted)
            .SelectMany(
                skill => SkillMarkdownCompiler.Compile(skill).SelectMany(
                    source => MarkdownChunker.Split(source.Markdown).Select(
                        (text, chunkIndex) => new PendingDocument(
                            skill.Id,
                            skill.Name,
                            source.RelativePath,
                            lastUpdates[skill.Id],
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
                (chunk, index) => new SkillSearchDocument(
                    chunk.SkillAggregateId,
                    chunk.SkillName,
                    chunk.SourcePath,
                    chunk.ChunkIndex,
                    chunk.Text,
                    embeddings[index]
                )
            )
            .ToList();

        var aggregateIds = skills
            .Select(skill => skill.Id)
            .Distinct()
            .ToList();
        var globalDocuments = documents
                .Select(
                    (document, index) => new KnowledgeSearchDocument(
                        KnowledgeSearchOwnerTypes.Skill,
                        document.SkillAggregateId,
                        KnowledgeSearchSourceTypes.Skill,
                        document.SourcePath,
                        document.ChunkIndex,
                        chunks[index].LastUpdateTimestamp,
                        KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
                        {
                            ["skillId"] = document.SkillAggregateId.Value.ToString(),
                            ["skillName"] = document.SkillName,
                            ["sourcePath"] = document.SourcePath,
                            ["updatedAt"] = chunks[index].LastUpdateTimestamp
                        }),
                        $"{document.SkillName} {document.SourcePath}",
                        document.Text,
                        document.Embedding
                    )
                )
                .ToList();
        await projectionWriter.Write(
            new SkillSearchProjectionBatch(
                aggregateIds,
                documents,
                globalDocuments
            )
        );
    }

    private sealed record PendingDocument(
        AggregateId SkillAggregateId,
        string SkillName,
        string SourcePath,
        DateTime LastUpdateTimestamp,
        int ChunkIndex,
        string Text
    )
    {
        public string EmbeddingText =>
            $"Skill: {SkillName}\nSource: {SourcePath}\n\n{Text}";
    }
}
