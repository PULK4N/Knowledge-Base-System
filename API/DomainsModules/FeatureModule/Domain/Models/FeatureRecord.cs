using System.Text.Json.Serialization;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeatureRecordId(Guid Value)
{
    public static FeatureRecordId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeatureRecordId FromDatabaseGuid(Guid value) =>
        new(value);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureRecordSourceType
{
    Other,
    Code,
    Web,
    Mcp
}

/// <summary>
/// Suggested record types. A type is free text stored in lower case, so these
/// are the common values rather than a closed list.
/// </summary>
public static class FeatureRecordTypes
{
    public const string Conversation = "conversation";
    public const string Research = "research";
    public const int MaximumLength = 50;

    public static string Normalize(string type) =>
        type.Trim().ToLowerInvariant();
}

/// <summary>
/// A titled Markdown note kept with a feature, such as a curated conversation
/// or a research finding. Plans link records by ID; a record does not know
/// which plans use it.
/// </summary>
public sealed class FeatureRecord
{
    private const int MaximumDerivedTitleLength = 80;

    public FeatureRecordId Id { get; init; }

    public string Type { get; set; } = FeatureRecordTypes.Conversation;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Where the record came from; null when it has no source.
    /// </summary>
    public FeatureRecordSourceType? SourceType { get; set; }

    /// <summary>
    /// A file path, URL, MCP tool name, or other provenance for SourceType.
    /// </summary>
    public string SourceReference { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Fills a record from the user message and AI answer of the conversation
    /// record events that predate titled Markdown records.
    /// </summary>
    internal void SetConversation(string userMessage, string aiAnswer)
    {
        Type = FeatureRecordTypes.Conversation;
        Title = DeriveTitle(userMessage);
        Content = $"## User\n\n{userMessage}\n\n## AI\n\n{aiAnswer}";
    }

    private static string DeriveTitle(string userMessage)
    {
        var firstLine = userMessage
            .Split('\n')
            .Select(line => line.Trim().TrimStart('#', '>', '-', '*', ' '))
            .FirstOrDefault(line => line.Length > 0);

        if (firstLine is null)
            return "Conversation";

        return firstLine.Length <= MaximumDerivedTitleLength
            ? firstLine
            : firstLine[..MaximumDerivedTitleLength].TrimEnd() + "…";
    }
}
