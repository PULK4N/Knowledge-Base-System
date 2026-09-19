using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;

namespace MemoryModule.Domain.Events;

public interface ICodexToolCallRecorded : IEvent;

public sealed record CodexToolCallRecordedV1(
    ThreadId ThreadId,
    PromptId PromptId,
    string ToolName,
    string ToolUseId,
    JsonElement Payload
) : ICodexToolCallRecorded
{
    public object Apply(
        object stateData,
        EventExecutionInfo eventExecutionInfo
    )
    {
        var state = (MemoryStateData)stateData;

        if (state.ChatPrompts.Count == 0)
            state.ThreadId = ThreadId;

        var toolCall = new CodexToolCallRecord
        {
            ToolName = ToolName,
            ToolUseId = ToolUseId,
            Payload = Payload
        };

        if (state.ChatPrompts.TryGetValue(PromptId, out var chatPrompt))
        {
            chatPrompt.CodexToolCalls.Add(toolCall);
            return state;
        }

        state.ChatPrompts.Add(
            PromptId,
            new ChatPrompt
            {
                PromptId = PromptId,
                PromptStartTimestamp = eventExecutionInfo.Timestamp,
                CodexToolCalls = [toolCall]
            }
        );

        return state;
    }
}
