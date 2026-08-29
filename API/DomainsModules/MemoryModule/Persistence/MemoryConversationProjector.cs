using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

/// <summary>
/// Projects memories as readable conversations for the user interface. It is
/// kept separate from <see cref="MemorySearchProjector"/> because a reader
/// needs every hook in order with its raw payload available, while retrieval
/// needs chunked and embedded text.
/// </summary>
public sealed class MemoryConversationProjector(
    IMemoryConversationRepository repository
) : IProjector
{
    private static readonly JsonSerializerOptions PayloadOptions =
        new() { WriteIndented = true };

    public Task Update(List<StateInfo> stateInfos)
    {
        var memories = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<MemoryStateData>()
            .ToList();
        var messages = memories
            .Where(memory => !memory.IsDeleted)
            .SelectMany(
                memory => memory.ChatPrompts.Values
                    .OrderBy(prompt => prompt.PromptStartTimestamp)
                    .ThenBy(prompt => prompt.PromptId.Value)
                    .SelectMany(
                        prompt => prompt.PromptHookRecords.Select(
                            (hook, hookIndex) => ToMessage(
                                memory,
                                prompt,
                                hook,
                                hookIndex
                            )
                        )
                    )
            )
            .ToList();

        return repository.Write(
            memories.Select(memory => memory.Id).Distinct().ToList(),
            messages
        );
    }

    private static MemoryConversationMessage ToMessage(
        MemoryStateData memory,
        ChatPrompt prompt,
        PromptHookRecord hook,
        int hookIndex
    )
    {
        var message = PromptHookPayload.Find(hook.Payload);

        return new MemoryConversationMessage(
            memory.Id,
            memory.ThreadId,
            prompt.PromptId,
            hookIndex,
            prompt.PromptStartTimestamp,
            hook.HookEventName,
            message?.Role ?? PromptHookMessageRoles.Hook,
            message?.Text ?? string.Empty,
            JsonSerializer.Serialize(hook.Payload, PayloadOptions)
        );
    }
}
