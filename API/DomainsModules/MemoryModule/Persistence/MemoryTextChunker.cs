using System.Text.Json;
using MemoryModule.Domain.Models;

namespace MemoryModule.Persistence;

public static class MemoryTextChunker
{
    public const int MaximumChunkLength = 2000;
    public const int ChunkOverlapLength = 500;

    public static IReadOnlyList<string> CompileChunks(
        ChatPrompt prompt,
        PromptHookRecord hook
    )
    {
        var text = string.Join(
            '\n',
            $"Hook: {hook.HookEventName}",
            $"Prompt started: {prompt.PromptStartTimestamp:O}",
            PromptHookPayload.FindMessage(hook.Payload) ?? CompilePayload(hook)
        );

        return Split(text);
    }

    private static string CompilePayload(PromptHookRecord hook) =>
        string.Join(
            '\n',
            "Payload:",
            JsonSerializer.Serialize(
                hook.Payload,
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

    public static IReadOnlyList<string> CompileSummaryChunks(
        ChatSummary summary
    )
    {
        var text = string.Join(
            '\n',
            "Chat summary",
            $"Summary created: {summary.SummaryTimestamp:O}",
            summary.Summary
        );

        return Split(text);
    }

    private static IReadOnlyList<string> Split(string text)
    {
        if (text.Length <= MaximumChunkLength)
            return [text];

        var chunks = new List<string>();
        var step = MaximumChunkLength - ChunkOverlapLength;

        for (var offset = 0; offset < text.Length; offset += step)
        {
            var length = Math.Min(MaximumChunkLength, text.Length - offset);
            chunks.Add(text.Substring(offset, length));

            if (offset + length == text.Length)
                break;
        }

        return chunks;
    }
}
