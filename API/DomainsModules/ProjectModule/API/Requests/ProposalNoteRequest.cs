namespace ProjectModule.API.Requests;

public sealed record ProposalNoteRequest
{
    public required string Title { get; init; }

    public string? Content { get; init; }
}
