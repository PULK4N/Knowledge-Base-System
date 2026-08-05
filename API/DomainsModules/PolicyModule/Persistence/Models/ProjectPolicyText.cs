namespace PolicyModule.Persistence.Models;

public sealed class ProjectPolicyText
{
    public int Id { get; set; }
    public Guid ProjectAggregateId { get; set; }
    public string Text { get; set; } = string.Empty;
}
