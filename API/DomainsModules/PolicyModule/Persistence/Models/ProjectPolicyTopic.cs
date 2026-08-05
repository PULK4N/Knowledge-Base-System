namespace PolicyModule.Persistence.Models;

public sealed class ProjectPolicyTopic
{
    public int Id { get; set; }
    public Guid ProjectAggregateId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int TopicOrder { get; set; }
}
