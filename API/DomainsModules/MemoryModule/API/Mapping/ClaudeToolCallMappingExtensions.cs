using System.Text.Json;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Mapping;

public static class ClaudeToolCallMappingExtensions
{
    public static void MapTo(
        this JsonElement payload,
        RecordClaudeToolCallCommand command
    )
    {
        command.ThreadId = new ThreadId(
            payload.GetProperty("session_id").GetGuid()
        );
        command.PromptId = new PromptId(
            payload.GetProperty("turn_id").GetGuid()
        );
        command.ToolName = payload.GetProperty("tool_name").GetString()!;
        command.ToolUseId = payload.GetProperty("tool_use_id").GetString()!;
        command.Payload = payload;
    }
}
