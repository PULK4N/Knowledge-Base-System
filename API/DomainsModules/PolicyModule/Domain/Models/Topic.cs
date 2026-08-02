namespace PolicyModule.Domain.Models;

public readonly record struct TopicName(string Name);

public sealed class Topic
{
    public TopicName TopicName { get; init; }
    public required string Description { get; init; }
    public Dictionary<PolicyId, Policy> Policies { get; } = [ ];
}
