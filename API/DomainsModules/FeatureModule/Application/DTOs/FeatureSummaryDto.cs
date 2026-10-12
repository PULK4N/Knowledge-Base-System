using FeatureModule.Persistence.Interfaces;

namespace FeatureModule.Application.DTOs;

public sealed record FeatureSummaryDto(
    Guid FeatureId,
    Guid ProjectId,
    string Name,
    string Summary,
    string Status,
    int PlanCount,
    int RecordCount
)
{
    public static FeatureSummaryDto FromReadModel(
        FeatureSummary readModel
    ) =>
        new(
            readModel.FeatureId,
            readModel.ProjectId,
            readModel.Name,
            readModel.Summary,
            readModel.Status,
            readModel.PlanCount,
            readModel.RecordCount
        );
}
