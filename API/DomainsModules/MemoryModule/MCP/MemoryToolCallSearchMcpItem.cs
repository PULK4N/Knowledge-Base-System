using MemoryModule.Application.DTOs;

namespace MemoryModule.MCP;

/// <summary>
/// A tool call found by memory_tool_call_search. A tool response can be very
/// large, so the payload is cut to the caller's limit and PayloadTruncated
/// says whether text was left out.
/// </summary>
public sealed record MemoryToolCallSearchMcpItem(
    Guid MemoryId,
    Guid ThreadId,
    Guid PromptId,
    int ToolCallIndex,
    DateTime Timestamp,
    string ToolName,
    string Description,
    string PayloadJson,
    bool PayloadTruncated
)
{
    public static MemoryToolCallSearchMcpItem FromDto(
        MemoryToolCallSearchItemDto toolCall,
        int payloadLength
    )
    {
        var truncated = toolCall.PayloadJson.Length > payloadLength;

        return new MemoryToolCallSearchMcpItem(
            toolCall.MemoryId,
            toolCall.ThreadId,
            toolCall.PromptId,
            toolCall.ToolCallIndex,
            toolCall.Timestamp,
            toolCall.ToolName,
            toolCall.Description,
            truncated
                ? toolCall.PayloadJson[..payloadLength]
                : toolCall.PayloadJson,
            truncated
        );
    }
}
