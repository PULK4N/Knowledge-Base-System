using EventSourcing.Shared.Models;

namespace ProjectModule.Domain.Models;

public readonly record struct ProposalNoteId(Guid Value)
{
    public static ProposalNoteId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static ProposalNoteId FromDatabaseGuid(Guid value) =>
        new(value);
}

/// <summary>An idea kept on the project to implement later.</summary>
public sealed class ProposalNote
{
    public ProposalNoteId Id { get; init; }

    public string Title { get; set; } = string.Empty;

    /// <summary>Markdown.</summary>
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
