using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Domain;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence;

public sealed class SkillSearchProjector(
    ITextEmbeddingGenerator embeddingGenerator,
    ISkillSearchRepository repository
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var skills = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<SkillStateData>()
            .ToList();
        var chunks = skills
            .Where(skill => !skill.IsDeleted)
            .SelectMany(
                skill => SkillMarkdownCompiler.Compile(skill).SelectMany(
                    source => MarkdownChunker.Split(source.Markdown).Select(
                        (text, chunkIndex) => new PendingDocument(
                            skill.Id,
                            skill.Name,
                            source.RelativePath,
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

        await repository.Replace(
            skills.Select(skill => skill.Id).Distinct().ToList(),
            documents
        );
    }

    private sealed record PendingDocument(
        AggregateId SkillAggregateId,
        string SkillName,
        string SourcePath,
        int ChunkIndex,
        string Text
    )
    {
        public string EmbeddingText =>
            $"Skill: {SkillName}\nSource: {SourcePath}\n\n{Text}";
    }
}
