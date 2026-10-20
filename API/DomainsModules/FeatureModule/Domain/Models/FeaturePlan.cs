using System.Text.Json.Serialization;
using EventSourcing.Shared.Models;

namespace FeatureModule.Domain.Models;

public readonly record struct FeaturePlanId(Guid Value)
{
    public static FeaturePlanId New() =>
        new(DatabaseFriendlyGuidGenerator.NewGuid());

    public static FeaturePlanId FromDatabaseGuid(Guid value) =>
        new(value);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeaturePlanContentType
{
    Markdown,

    Html
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeaturePlanStatus
{
    NotStarted,

    InProgress,

    Delayed,

    Completed
}

/// <summary>
/// A source-control branch used by a plan, with the branches created from it.
/// Its name is unique within the plan's branch tree.
/// </summary>
public sealed class FeaturePlanBranch
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<FeaturePlanBranch> SubBranches { get; set; } = [];

    public IEnumerable<FeaturePlanBranch> SelfAndDescendants() =>
        SubBranches
            .SelectMany(subBranch => subBranch.SelfAndDescendants())
            .Prepend(this);
}

public sealed class FeaturePlan
{
    public FeaturePlanId Id { get; init; }

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public FeaturePlanContentType ContentType { get; set; }

    /// <summary>
    /// Short context returned with the feature so an AI can decide whether
    /// the plan content is worth loading, for example research-only plans.
    /// </summary>
    public string AdditionalInfo { get; set; } = string.Empty;

    public FeaturePlanStatus Status { get; set; }

    /// <summary>
    /// The plan's base branch. It is null only for plans created before
    /// plans required a base branch.
    /// </summary>
    public FeaturePlanBranch? Branch { get; set; }

    /// <summary>
    /// The plan done before this one. Its base branch is where this plan's base
    /// branch was checked out from; null means it came from the feature branch.
    /// </summary>
    public FeaturePlanId? PreviousPlanId { get; set; }

    /// <summary>
    /// Records of the same feature that this plan uses. Only the plan keeps the
    /// link; removing a record removes it from here.
    /// </summary>
    public List<FeatureRecordId> RecordIds { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public FeaturePlanBranch? FindBranch(string name) =>
        Branch?.SelfAndDescendants()
            .SingleOrDefault(branch => branch.Name == name);

    public FeaturePlanBranch? FindParentBranch(string name) =>
        Branch?.SelfAndDescendants()
            .SingleOrDefault(
                branch => branch.SubBranches.Any(subBranch => subBranch.Name == name)
            );
}
