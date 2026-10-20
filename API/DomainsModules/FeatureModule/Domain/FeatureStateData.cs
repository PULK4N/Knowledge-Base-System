using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain;

public sealed class FeatureStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;

    public List<MemoryHistoryRecord> MemoryHistory { get; set; } = [];

    public bool IsDeleted { get; set; }

    public AggregateId ProjectId { get; set; }

    public AggregateId? ParentFeatureId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Free-form AI-written progress context, not a workflow state or filtering value.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Skills that may help an AI understand or work on this feature.
    /// </summary>
    public List<AggregateId> RelatedSkillIds { get; set; } = [];

    /// <summary>
    /// Titled Markdown records, such as curated conversations or research
    /// findings, typed by free text such as "conversation" or "research".
    /// </summary>
    public List<FeatureRecord> Records { get; set; } = [];

    /// <summary>
    /// Reviewed decisions that intended behaviour is not a defect, so later reviews
    /// do not report the same finding again.
    /// </summary>
    public List<FeatureReviewNote> ReviewNotes { get; set; } = [];

    public List<FeaturePlan> Plans { get; set; } = [];

    /// <summary>
    /// Null until the feature branch is created; features are often written before it.
    /// </summary>
    public FeatureBranch? Branch { get; set; }

    /// <summary>
    /// Branch where functionality is combined and deployed for Analyst checking before review finishes.
    /// </summary>
    public string? CompositeBranchName { get; set; }

    /// <summary>
    /// Legacy current-plan selection kept only so released current-plan events replay.
    /// New plan events do not read or change it.
    /// </summary>
    [Obsolete("Replay only. Current-plan selection was removed.")]
    public FeaturePlanId? CurrentPlanId { get; set; }
}
