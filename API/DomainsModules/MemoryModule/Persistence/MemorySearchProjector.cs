using EmbeddingModule;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySearchProjector(
    IProjectionEmbeddingGenerator embeddingGenerator,
    IMemorySearchProjectionWriter projectionWriter
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
                                    MemoryModelNames.Normalize(prompt.Model),
                                    text
                                )
                            )
                    )
                )
            )
            .ToList();
        // Every summary is indexed, not only the latest one. HookIndex holds
        // the summary's position so each summary keeps its own rows; search
        // still groups results by memory session.
        var summaryChunks = memories
            .Where(memory => !memory.IsDeleted)
            .SelectMany(
                memory => memory.ChatSummaries
                    .Select(
                        (summary, summaryIndex) => (summary, summaryIndex)
                    )
                    .Where(
                        item => !string.IsNullOrWhiteSpace(
                            item.summary.Summary
                        )
                    )
                    .SelectMany(
                        item => MemoryTextChunker
                            .CompileSummaryChunks(item.summary)
                            .Select(
                                (text, chunkIndex) => new PendingDocument(
                                    memory.Id,
                                    memory.ThreadId,
                                    new PromptId(Guid.Empty),
                                    item.summaryIndex,
                                    chunkIndex,
                                    item.summary.SummaryTimestamp,
                                    MemorySearchDocumentSources.ChatSummary,
                                    null,
                                    text
                                )
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
                    chunk.Model,
                    chunk.Text,
                    embeddings[index]
                )
            )
            .ToList();

        var aggregateIds = memories
            .Select(memory => memory.Id)
            .Distinct()
            .ToList();
        await projectionWriter.Write(
            new MemorySearchProjectionBatch(
                aggregateIds,
                documents,
                documents.Select(ToKnowledgeDocument).ToList()
            )
        );
    }

    private static KnowledgeSearchDocument ToKnowledgeDocument(
        MemorySearchDocument document
    )
    {
        var isSummary = string.Equals(
            document.HookEventName,
            MemorySearchDocumentSources.ChatSummary,
            StringComparison.Ordinal
        );

        return new KnowledgeSearchDocument(
            KnowledgeSearchOwnerTypes.Memory,
            document.MemoryAggregateId,
            isSummary
                ? KnowledgeSearchSourceTypes.MemorySummary
                : KnowledgeSearchSourceTypes.MemoryPrompt,
            isSummary
                ? $"summary:{document.HookIndex}"
                : $"{document.PromptId.Value:N}:{document.HookIndex}",
            document.ChunkIndex,
            document.PromptStartTimestamp,
            KnowledgeSearchMetadata.Create(new Dictionary<string, object?>
            {
                ["memoryId"] = document.MemoryAggregateId.Value.ToString(),
                ["threadId"] = document.ThreadId.Value.ToString(),
                ["promptId"] = document.PromptId.Value.ToString(),
                ["hookIndex"] = document.HookIndex,
                ["hookEventName"] = document.HookEventName,
                ["timestamp"] = document.PromptStartTimestamp
            }),
            document.HookEventName,
            document.Text,
            document.Embedding
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
        string? Model,
        string Text
    );
}
