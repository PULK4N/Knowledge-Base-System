using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IClaudePromptHookRecorded : IEvent;

public sealed record ClaudePromptHookRecordedV1(
    ThreadId ThreadId,
    PromptId PromptId,
    string HookEventName,
    JsonElement Payload
) : IClaudePromptHookRecorded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        if (state.ChatPrompts.Count == 0)
            state.ThreadId = ThreadId;

        var hookRecord = new PromptHookRecord
        {
            HookEventName = HookEventName,
            Payload = Payload
        };

        if (state.ChatPrompts.TryGetValue(PromptId, out var chatPrompt))
        {
            chatPrompt.PromptHookRecords.Add(hookRecord);
            return state;
        }

        state.ChatPrompts.Add(
            PromptId,
            new ChatPrompt
            {
                PromptId = PromptId,
                PromptStartTimestamp = eventExecutionInfo.Timestamp,
                PromptHookRecords = [hookRecord]
            }
        );

        return state;
    }
}
