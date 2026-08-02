using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface ICodexMemoryMigrated : IEvent;

public sealed record CodexMemoryMigratedV1(
    ThreadId ThreadId,
    PromptId PromptId,
    JsonElement Payload
) : ICodexMemoryMigrated
{
    public const string UserMigrationHookEventName =
        "user_memory_migration";

    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        state.ThreadId = ThreadId;
        state.ChatPrompts.Add(
            PromptId,
            new ChatPrompt
            {
                PromptId = PromptId,
                PromptStartTimestamp = eventExecutionInfo.Timestamp,
                PromptHookRecords =
                [
                    new PromptHookRecord
                    {
                        HookEventName = UserMigrationHookEventName,
                        Payload = Payload
                    }
                ]
            }
        );

        return state;
    }
}
