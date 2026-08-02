using System.Text.Json;
using MemoryModule.Application.Commands;
using MemoryModule.Domain.Models;

namespace MemoryModule.API.Mapping;

public static class CodexPromptHookMappingExtensions
{
    public static void MapTo(
        this JsonElement payload,
        RecordCodexPromptHookCommand command
    )
    {
        command.ThreadId = new ThreadId(
            payload.GetProperty("session_id").GetGuid()
        );
        command.PromptId = new PromptId(
            payload.GetProperty("turn_id").GetGuid()
        );
        command.HookEventName = payload
            .GetProperty("hook_event_name")
            .GetString()!;
        command.Payload = payload;
    }
}
