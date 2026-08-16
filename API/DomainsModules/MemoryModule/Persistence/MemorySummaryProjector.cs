using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

public sealed class MemorySummaryProjector(
    IMemorySummaryRepository repository
) : IProjector
{
    public Task Update(List<StateInfo> stateInfos)
    {
        var memories = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<MemoryStateData>()
            .ToList();
        var summaries = memories
            .Where(memory => !memory.IsDeleted)
            .Select(ToReadModel)
            .ToList();

        return repository.Write(
            memories.Select(memory => memory.Id).Distinct().ToList(),
            summaries
        );
    }

    private static MemorySummary ToReadModel(MemoryStateData memory)
    {
        var prompts = memory.ChatPrompts.Values
            .OrderBy(prompt => prompt.PromptStartTimestamp)
            .ThenBy(prompt => prompt.PromptId.Value)
            .ToList();
        var firstPromptTimestamp = prompts.Count == 0
            ? (DateTime?)null
            : prompts[0].PromptStartTimestamp;
        var lastPromptTimestamp = prompts.Count == 0
            ? (DateTime?)null
            : prompts[^1].PromptStartTimestamp;
        var summaryTimestamp = string.IsNullOrWhiteSpace(
            memory.ChatSummary.Summary
        )
            ? (DateTime?)null
            : memory.ChatSummary.SummaryTimestamp;
        var lastActivityTimestamp = lastPromptTimestamp is null
            ? summaryTimestamp ?? DateTime.MinValue
            : summaryTimestamp > lastPromptTimestamp
                ? summaryTimestamp.Value
                : lastPromptTimestamp.Value;

        return new MemorySummary(
            memory.Id,
            memory.ThreadId,
            memory.ChatSummary.Summary,
            prompts.Count,
            firstPromptTimestamp,
            lastPromptTimestamp,
            summaryTimestamp,
            lastActivityTimestamp
        );
    }
}
