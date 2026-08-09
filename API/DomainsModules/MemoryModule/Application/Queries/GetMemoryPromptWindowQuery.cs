using System.Text.Json;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Application.DTOs;
using MemoryModule.Domain.Models;

namespace MemoryModule.Application.Queries;

public sealed class GetMemoryPromptWindowQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : MemoryQuery<MemoryPromptWindowResult?>(stateCalculator, eventStore)
{
    public const int DefaultMaximumTokens = 2000;
    public const int MinimumMaximumTokens = 128;
    public const int MaximumMaximumTokens = 4000;
    public const int MaximumPromptsPerDirection = 10;

    public required Guid MemoryId { get; set; }
    public Guid PromptId { get; set; }
    public int Before { get; set; } = 1;
    public int After { get; set; } = 1;
    public int MaxTokens { get; set; } = DefaultMaximumTokens;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(
            MemoryId != Guid.Empty
            && Before is >= 0 and <= MaximumPromptsPerDirection
            && After is >= 0 and <= MaximumPromptsPerDirection
            && MaxTokens is >= MinimumMaximumTokens
                and <= MaximumMaximumTokens
        );

    protected override async Task<MemoryPromptWindowResult?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(MemoryId);
        var states = await GetStates([aggregateId]);

        if (!states.TryGetValue(aggregateId, out var state))
            return null;

        var prompts = state.ChatPrompts.Values
            .OrderBy(prompt => prompt.PromptStartTimestamp)
            .ThenBy(prompt => prompt.PromptId.Value)
            .ToList();
        if (prompts.Count == 0)
            return null;

        var anchorIndex = PromptId == Guid.Empty
            ? prompts.Count - 1
            : prompts.FindIndex(prompt => prompt.PromptId.Value == PromptId);
        if (anchorIndex < 0)
            return null;

        var firstIndex = Math.Max(0, anchorIndex - Before);
        var lastIndex = Math.Min(
            prompts.Count - 1,
            anchorIndex + After
        );
        var message =
            $"Returned prompts around anchor '{prompts[anchorIndex].PromptId.Value}'.";
        var budget = new ApproximateTokenBudget(MaxTokens);
        budget.Take(message);
        var selected = SelectWithinBudget(
            prompts,
            anchorIndex,
            firstIndex,
            lastIndex,
            budget
        );

        return new MemoryPromptWindowResult(
            message,
            state.Id.Value,
            state.ThreadId.Value,
            prompts[anchorIndex].PromptId.Value,
            selected,
            firstIndex > 0,
            lastIndex < prompts.Count - 1,
            budget.ApproximateTokenCount,
            budget.IsTruncated
        );
    }

    private static List<MemoryPromptDto> SelectWithinBudget(
        List<ChatPrompt> prompts,
        int anchorIndex,
        int firstIndex,
        int lastIndex,
        ApproximateTokenBudget budget
    )
    {
        var priorityIndexes = new List<int> { anchorIndex };
        var maximumDistance = Math.Max(
            anchorIndex - firstIndex,
            lastIndex - anchorIndex
        );

        for (var distance = 1; distance <= maximumDistance; distance++)
        {
            if (anchorIndex - distance >= firstIndex)
                priorityIndexes.Add(anchorIndex - distance);
            if (anchorIndex + distance <= lastIndex)
                priorityIndexes.Add(anchorIndex + distance);
        }

        return priorityIndexes
            .Select(
                index =>
                {
                    var prompt = prompts[index];
                    var text = budget.Take(Compile(prompt));

                    return text is null
                        ? null
                        : new MemoryPromptDto(
                            prompt.PromptId.Value,
                            prompt.PromptStartTimestamp,
                            text
                        );
                }
            )
            .OfType<MemoryPromptDto>()
            .OrderBy(prompt => prompt.PromptStartTimestamp)
            .ThenBy(prompt => prompt.PromptId)
            .ToList();
    }

    private static string Compile(ChatPrompt prompt) =>
        string.Join(
            "\n\n",
            prompt.PromptHookRecords.Select(
                hook => string.Join(
                    '\n',
                    $"Hook: {hook.HookEventName}",
                    "Payload:",
                    JsonSerializer.Serialize(hook.Payload)
                )
            )
        );
}
