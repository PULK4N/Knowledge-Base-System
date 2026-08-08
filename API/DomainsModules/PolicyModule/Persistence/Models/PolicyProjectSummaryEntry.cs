namespace PolicyModule.Persistence.Models;

public sealed class PolicyProjectSummaryEntry
{
    public int Id { get; set; }
    public required Guid ProjectAggregateId { get; set; }
    public required string ProjectName { get; set; }
    public required string RepositoryPathsJson { get; set; }
}
