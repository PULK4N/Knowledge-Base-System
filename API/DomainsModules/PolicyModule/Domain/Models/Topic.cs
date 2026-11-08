namespace PolicyModule.Domain.Models;

public sealed class Topic
{
    public TopicName TopicName { get; init; }
    public required string Description { get; init; }
    public DateTime UpdatedAt { get; set; }
    public Dictionary<PolicyId, Policy> Policies { get; } = [];
}
