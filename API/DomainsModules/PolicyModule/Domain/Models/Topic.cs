namespace PolicyModule.Domain.Models;

public readonly record struct TopicName(string Name);

public class Topic
{
    public TopicName TopicName { get; set; }
    public required string Description { get; set; }
    public Dictionary<PolicyId, Policy> Policies { get; } = new Dictionary<PolicyId, Policy>();
}
