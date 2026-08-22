using EventSourcing.Shared.Models;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain;

public sealed class FeatureStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;

    public bool IsDeleted { get; set; }

    public AggregateId ProjectId { get; set; }

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
    /// Curated user and AI exchanges that explain decisions, rather than raw transcripts.
    /// </summary>
    public List<FeatureRecord> Records { get; set; } = [];

    /// <summary>
    /// Retains both the selected plan and previous plans so any retained plan can be selected again.
    /// </summary>
    public List<FeaturePlan> Plans { get; set; } = [];

    /// <summary>
    /// Selects the current plan from <see cref="Plans"/>; all other retained plans are previous plans.
    /// </summary>
    public FeaturePlanId? CurrentPlanId { get; set; }
}
