using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.DTOs;

public sealed record SkillSummaryDto(
    Guid SkillId,
    string Name
)
{
    public static SkillSummaryDto FromReadModel(
        SkillSummary readModel
    ) =>
        new(readModel.SkillId, readModel.Name);
}
