using EventSourcing.Shared.Models;
using PolicyModule.Domain.Models;

namespace PolicyModule.Domain;

public class ProjectPoliciesStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public List<string> RepositoryPaths { get; set; } = [];
    public bool IsDeleted { get; set; }
    public Dictionary<PolicyId, Policy> Policies { get; } = new Dictionary<PolicyId, Policy>();
    public List<TopicName> RelatedTopics { get; } = new List<TopicName>();
}
