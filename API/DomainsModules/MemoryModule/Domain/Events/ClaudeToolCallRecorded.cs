using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface IClaudeToolCallRecorded : IEvent;

public sealed record ClaudeToolCallRecordedV1(
    ThreadId ThreadId,
    PromptId PromptId,
    string ToolName,
    string ToolUseId,
    JsonElement Payload
) : IClaudeToolCallRecorded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        if (state.ChatPrompts.Count == 0)
            state.ThreadId = ThreadId;

        var toolCall = new ToolCallRecord
        {
            ToolName = ToolName,
            ToolUseId = ToolUseId,
            Payload = Payload
        };

        if (state.ChatPrompts.TryGetValue(PromptId, out var chatPrompt))
        {
            chatPrompt.ToolCalls.Add(toolCall);
            return state;
        }

        state.ChatPrompts.Add(
            PromptId,
            new ChatPrompt
            {
                PromptId = PromptId,
                PromptStartTimestamp = eventExecutionInfo.Timestamp,
                ToolCalls = [toolCall]
            }
        );

        return state;
    }
}
