using ProjectModule.Shared;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Models;

namespace ProjectModule.Domain;

public class ProjectStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public List<string> RepositoryPaths { get; set; } = [];
    public List<MemoryHistoryRecord> MemoryHistory { get; set; } = [];

    public bool IsDeleted { get; set; }
    public Dictionary<PolicyId, Policy> Policies { get; } = new Dictionary<PolicyId, Policy>();
    public List<TopicName> RelatedTopics { get; } = new List<TopicName>();
    public List<AggregateId> RelatedSkillIds { get; } = [];

    /// <summary>Review settings per linked skill; links written before review data have none.</summary>
    public Dictionary<AggregateId, ProjectSkillReview> SkillReviews { get; } = [];

    public Dictionary<ReviewLevel, ReviewModel> ReviewLevelModels { get; set; } = [];

    public Dictionary<ProposalNoteId, ProposalNote> ProposalNotes { get; } = [];
}
