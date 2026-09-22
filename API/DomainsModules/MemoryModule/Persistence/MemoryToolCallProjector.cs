using System.Text.Json;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using MemoryModule.Domain;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace MemoryModule.Persistence;

/// <summary>
/// Projects recorded tool invocations as plain rows. It is kept separate from
/// <see cref="MemorySearchProjector"/> because tool calls are read by prompt
/// and tool name and carry no embeddings. Only the tool input and the tool
/// response are projected; the rest of the hook payload describes the session
/// rather than the invocation.
/// </summary>
public sealed class MemoryToolCallProjector(
    IMemoryToolCallRepository repository
) : IProjector
{
    private static readonly JsonSerializerOptions PayloadOptions =
        new() { WriteIndented = true };
    private static readonly List<string> ProjectedPayloadProperties =
    [
        "tool_input",
        "tool_response"
    ];

    public Task Update(List<StateInfo> stateInfos)
    {
        var memories = stateInfos
            .Select(stateInfo => stateInfo.StateData)
            .OfType<MemoryStateData>()
            .ToList();
        var toolCalls = memories
            .Where(memory => !memory.IsDeleted)
            .SelectMany(
                memory => memory.ChatPrompts.Values
                    .OrderBy(prompt => prompt.PromptStartTimestamp)
                    .ThenBy(prompt => prompt.PromptId.Value)
                    .SelectMany(
                        prompt => prompt.ToolCalls.Select(
                            (toolCall, toolCallIndex) => ToToolCall(
                                memory,
                                prompt,
                                toolCall,
                                toolCallIndex
                            )
                        )
                    )
            )
            .ToList();

        return repository.Write(
            memories.Select(memory => memory.Id).Distinct().ToList(),
            toolCalls
        );
    }

    private static MemoryToolCall ToToolCall(
        MemoryStateData memory,
        ChatPrompt prompt,
        ToolCallRecord toolCall,
        int toolCallIndex
    ) =>
        new(
            memory.Id,
            memory.ThreadId,
            prompt.PromptId,
            toolCallIndex,
            prompt.PromptStartTimestamp,
            toolCall.ToolName,
            toolCall.ToolUseId,
            ToDescription(toolCall.Payload),
            ToPayloadJson(toolCall.Payload)
        );

    /// Reads the description the agent wrote for the invocation, which reads
    /// better in a list than the tool use id.
    private static string ToDescription(JsonElement payload)
    {
        if (
            payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("tool_input", out var toolInput)
            && toolInput.ValueKind == JsonValueKind.Object
            && toolInput.TryGetProperty("description", out var description)
            && description.ValueKind == JsonValueKind.String
        )
            return description.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static string ToPayloadJson(JsonElement payload)
    {
        var projected = new Dictionary<string, JsonElement>();

        if (payload.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in ProjectedPayloadProperties)
            {
                if (payload.TryGetProperty(propertyName, out var value))
                    projected.Add(propertyName, value);
            }
        }

        return JsonSerializer.Serialize(projected, PayloadOptions);
    }
}
