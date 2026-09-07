namespace FeatureModule.Application.Models;

public sealed record FeatureCommandResult(string Status)
{
    public static FeatureCommandResult Ok { get; } = new("OK");
}

public sealed record FeatureCreatedCommandResult(
    string Status,
    Guid FeatureId
)
{
    public static FeatureCreatedCommandResult Ok(Guid featureId) =>
        new("OK", featureId);
}

public sealed record FeatureRecordCreatedCommandResult(
    string Status,
    Guid RecordId
)
{
    public static FeatureRecordCreatedCommandResult Ok(Guid recordId) =>
        new("OK", recordId);
}

public sealed record FeatureReviewNoteCreatedCommandResult(
    string Status,
    Guid ReviewNoteId
)
{
    public static FeatureReviewNoteCreatedCommandResult Ok(Guid reviewNoteId) =>
        new("OK", reviewNoteId);
}

public sealed record FeatureResearchDiscoveryCreatedCommandResult(
    string Status,
    Guid DiscoveryId
)
{
    public static FeatureResearchDiscoveryCreatedCommandResult Ok(
        Guid discoveryId
    ) =>
        new("OK", discoveryId);
}

public sealed record FeaturePlanCreatedCommandResult(
    string Status,
    Guid PlanId
)
{
    public static FeaturePlanCreatedCommandResult Ok(Guid planId) =>
        new("OK", planId);
}
