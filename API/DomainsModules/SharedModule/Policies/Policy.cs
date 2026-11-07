namespace SharedModule.Policies;

public sealed record Policy
{
    public PolicyId PolicyId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}
