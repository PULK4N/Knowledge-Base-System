using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySearchProjector(
    ITextEmbeddingGenerator embeddingGenerator,
    IMemorySearchRepository repository
) : IProjector
{
    public async Task Update(List<StateInfo> stateInfos)
    {
        var memories = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<MemoryStateData>()
            .ToList();
        var promptChunks = memories
            .Where(memory => !memory.IsDeleted)
            .SelectMany(
                memory => memory.ChatPrompts.Values.SelectMany(
                    prompt => prompt.PromptHookRecords.SelectMany(
                        (hook, hookIndex) => MemoryTextChunker
                            .CompileChunks(prompt, hook)
                            .Select(
                                (text, chunkIndex) => new PendingDocument(
                                    memory.Id,
                                    memory.ThreadId,
                                    prompt.PromptId,
                                    hookIndex,
                                    chunkIndex,
                                    prompt.PromptStartTimestamp,
                                    hook.HookEventName,
                                    text
                                )
                            )
                    )
                )
            )
            .ToList();
        var summaryChunks = memories
            .Where(
                memory =>
                    !memory.IsDeleted
                    && !string.IsNullOrWhiteSpace(
                        memory.ChatSummary.Summary
                    )
            )
            .SelectMany(
                memory => MemoryTextChunker
                    .CompileSummaryChunks(memory.ChatSummary)
                    .Select(
                        (text, chunkIndex) => new PendingDocument(
                            memory.Id,
                            memory.ThreadId,
                            new PromptId(Guid.Empty),
                            0,
                            chunkIndex,
                            memory.ChatSummary.SummaryTimestamp,
                            MemorySearchDocumentSources.ChatSummary,
                            text
                        )
                    )
            )
            .ToList();
        var chunks = promptChunks
            .Concat(summaryChunks)
            .ToList();
        var embeddings = await embeddingGenerator.Generate(
            chunks.Select(chunk => chunk.Text).ToList()
        );
        var documents = chunks
            .Select(
                (chunk, index) => new MemorySearchDocument(
                    chunk.MemoryAggregateId,
                    chunk.ThreadId,
                    chunk.PromptId,
                    chunk.HookIndex,
                    chunk.ChunkIndex,
                    chunk.PromptStartTimestamp,
                    chunk.HookEventName,
                    chunk.Text,
                    embeddings[index]
                )
            )
            .ToList();

        await repository.Write(
            memories.Select(memory => memory.Id).Distinct().ToList(),
            documents
        );
    }

    private sealed record PendingDocument(
        AggregateId MemoryAggregateId,
        ThreadId ThreadId,
        PromptId PromptId,
        int HookIndex,
        int ChunkIndex,
        DateTime PromptStartTimestamp,
        string HookEventName,
        string Text
    );
}
