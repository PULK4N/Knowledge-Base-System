using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using SkillsModule.Domain;
using SkillsModule.Domain.Models;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Persistence.Tests;

public sealed class SkillSearchProjectorTests
{
    private static readonly AggregateId SkillId =
        AggregateId.FromDatabaseGuid(
            Guid.Parse("11111111-1111-1111-1111-111111111111")
        );

    [Fact]
    public async Task Update_projects_all_references_regardless_of_automatic_loading_but_not_attachments()
    {
        var state = CreateSkill();
        var embeddingGenerator = new FakeEmbeddingGenerator();
        var repository = new FakeRepository();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var projector = new SkillSearchProjector(
            embeddingGenerator,
            repository,
            knowledgeRepository,
            new ImmediateTransaction()
        );

        await projector.Update([CreateStateInfo(state)]);

        Assert.Equal([SkillId], repository.AggregateIds);
        Assert.Contains(
            repository.Documents,
            document =>
                document.SourcePath == SkillMarkdownCompiler.MainSkillPath
                && document.Text.Contains("# Usage")
        );
        Assert.Contains(
            repository.Documents,
            document =>
                document.SourcePath == "references/architecture.md"
                && document.Text.Contains("# Architecture")
        );
        Assert.Contains(
            repository.Documents,
            document =>
                document.SourcePath == "references/usage.md"
                && document.Text.Contains("# Usage reference")
        );
        Assert.All(
            repository.Documents,
            document => Assert.Equal([1f, 2f], document.Embedding.ToArray())
        );
        Assert.Equal(repository.Documents.Count, knowledgeRepository.Documents.Count);
        Assert.All(
            knowledgeRepository.Documents,
            document => Assert.Equal([1f, 2f], document.Embedding.ToArray())
        );
        Assert.DoesNotContain(
            repository.Documents,
            document => document.Text.Contains("SECRET-ATTACHMENT.pdf")
        );
        Assert.DoesNotContain(
            embeddingGenerator.LastInputs,
            input => input.Contains("SECRET-ATTACHMENT.pdf")
        );
        Assert.All(
            repository.Documents,
            document => Assert.Contains(
                $"Skill: {document.SkillName}",
                embeddingGenerator.LastInputs.Single(
                    input => input.EndsWith(document.Text, StringComparison.Ordinal)
                )
            )
        );
    }

    [Fact]
    public async Task Update_replaces_deleted_skill_with_empty_snapshot()
    {
        var state = CreateSkill();
        state.IsDeleted = true;
        var repository = new FakeRepository();
        var knowledgeRepository = new FakeKnowledgeRepository();
        var projector = new SkillSearchProjector(
            new FakeEmbeddingGenerator(),
            repository,
            knowledgeRepository,
            new ImmediateTransaction()
        );

        await projector.Update([CreateStateInfo(state)]);

        Assert.Equal([SkillId], repository.AggregateIds);
        Assert.Empty(repository.Documents);
        Assert.Equal([SkillId], knowledgeRepository.OwnerAggregateIds);
        Assert.Empty(knowledgeRepository.Documents);
    }

    [Fact]
    public void Split_starts_new_chunks_at_markdown_headings()
    {
        const string markdown = "Preamble\n\n# First\nFirst content\n\n## Second\nSecond content";

        var chunks = MarkdownChunker.Split(markdown);

        Assert.Equal(3, chunks.Count);
        Assert.Equal("Preamble", chunks[0]);
        Assert.StartsWith("# First", chunks[1]);
        Assert.StartsWith("## Second", chunks[2]);
    }

    [Fact]
    public void Split_uses_overlapping_bounded_chunks_for_oversized_sections()
    {
        var markdown = $"# Large\n{new string('x', 4_000)}";

        var chunks = MarkdownChunker.Split(markdown);

        Assert.True(chunks.Count > 1);
        Assert.All(
            chunks,
            chunk => Assert.InRange(
                chunk.Length,
                1,
                MarkdownChunker.MaximumChunkLength
            )
        );
        Assert.Equal(
            chunks[0][^MarkdownChunker.ChunkOverlapLength..],
            chunks[1][..MarkdownChunker.ChunkOverlapLength]
        );
    }

    private static SkillStateData CreateSkill()
    {
        var state = new SkillStateData(SkillId)
        {
            Name = "event-sourcing",
            Description = "Build event-sourced features",
            Content = "# Usage\nUse immutable events.\n\n## Validation\nValidate before applying.",
            Tags = ["events", "dotnet"]
        };
        state.References.Add(
            "references/architecture.md",
            new SkillReference2(
                "# Architecture\nKeep domain and persistence separate.",
                false
            )
        );
        state.References.Add(
            "references/usage.md",
            new SkillReference2(
                "# Usage reference\nLoad this automatically.",
                true
            )
        );
        state.Attachments.Add(
            FileId.FromDatabaseGuid(
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            ),
            new Attachment
            {
                Id = FileId.FromDatabaseGuid(
                    Guid.Parse("22222222-2222-2222-2222-222222222222")
                ),
                Name = "SECRET-ATTACHMENT.pdf",
                Size = 1024,
                FileType = "application/pdf",
                Extension = "pdf"
            }
        );

        return state;
    }

    private static StateInfo CreateStateInfo(SkillStateData state) =>
        StateInfo.Create(state, "skills-state-machine", state.Id);

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

    private sealed class FakeRepository : ISkillSearchRepository
    {
        public IReadOnlyCollection<AggregateId> AggregateIds { get; private set; } = [];
        public IReadOnlyCollection<SkillSearchDocument> Documents { get; private set; } = [];

        public Task Write(
            IReadOnlyCollection<AggregateId> skillAggregateIds,
            IReadOnlyCollection<SkillSearchDocument> documents,
            CancellationToken cancellationToken = default
        )
        {
            AggregateIds = skillAggregateIds;
            Documents = documents;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<SkillSearchCandidate>> SearchText(
            string query,
            int candidateCount,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<SkillSearchCandidate>> SearchVector(
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
