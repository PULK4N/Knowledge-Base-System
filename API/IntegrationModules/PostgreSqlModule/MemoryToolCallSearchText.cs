using System.Text;
using System.Text.Json;

namespace PostgreSqlModule;

/// <summary>
/// Extracts the searchable text of a tool call's input and response from its
/// projected payload. Only JSON values are kept, so property names such as
/// "command" or "stdout" do not match every call. The text is capped because
/// PostgreSQL rejects a tsvector above 1 MB, and one oversized tool response
/// would otherwise fail the whole projection write.
/// PostgreSQL keeps a Unix path such as /src/api/Program.cs as one token, so
/// the parts of every word containing a slash are added as extra words. A
/// search for Program.cs then matches the full path.
/// </summary>
internal static class MemoryToolCallSearchText
{
    internal const int MaximumLength = 100_000;

    private const string InputProperty = "tool_input";
    private const string OutputProperty = "tool_response";

    internal static (string Input, string Output) From(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        var payload = document.RootElement;

        return (
            Extract(payload, InputProperty),
            Extract(payload, OutputProperty)
        );
    }

    private static string Extract(JsonElement payload, string propertyName)
    {
        if (
            payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty(propertyName, out var value)
        )
            return string.Empty;

        var text = new StringBuilder();
        Append(value, text);

        return text.Length > MaximumLength
            ? text.ToString(0, MaximumLength)
            : text.ToString();
    }

    private static void Append(JsonElement value, StringBuilder text)
    {
        if (text.Length >= MaximumLength)
            return;

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                    Append(property.Value, text);
                break;
            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                    Append(item, text);
                break;
            case JsonValueKind.String:
                var stringValue = value.GetString();
                AppendLine(stringValue, text);
                AppendLine(PathParts(stringValue), text);
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AppendLine(value.GetRawText(), text);
                break;
        }
    }

    private static string? PathParts(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('/'))
            return null;

        var parts = value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Contains('/'))
            .Select(word => word.Replace('/', ' '));

        return string.Join(' ', parts);
    }

    private static void AppendLine(string? value, StringBuilder text)
    {
        if (string.IsNullOrEmpty(value))
            return;

        if (text.Length > 0)
            text.Append('\n');
        text.Append(value);
    }
}
