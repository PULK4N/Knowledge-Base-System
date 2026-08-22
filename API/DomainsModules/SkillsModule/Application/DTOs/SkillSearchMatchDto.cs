using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.DTOs;

public sealed record SkillSearchMatchDto(
    Guid SkillId,
    string Name,
    string SourcePath,
    int ChunkIndex,
    string Text,
    double Score,
    int? TextRank,
    int? VectorRank
)
{
    public static SkillSearchMatchDto FromSearchResult(
        SkillSearchResult result
    ) =>
        new(
            result.Skill.SkillAggregateId.Value,
            result.Skill.SkillName,
            result.Skill.SourcePath,
            result.Skill.ChunkIndex,
            result.Skill.Text,
            result.Score,
            result.TextRank,
            result.VectorRank
        );
}
