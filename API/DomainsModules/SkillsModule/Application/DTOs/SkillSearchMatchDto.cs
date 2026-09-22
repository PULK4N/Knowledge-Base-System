using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.DTOs;

/// <summary>
/// Search response carrying the ranked top matches and, separately, the best
/// match per distinct skill, so repeated chunks of one skill cannot hide the
/// other skills that matched.
/// </summary>
public sealed record SkillSearchResultsDto(
    List<SkillSearchMatchDto> TopMatches,
    List<SkillSearchMatchDto> DistinctSources
)
{
    public static SkillSearchResultsDto FromSearchResults(
        SkillSearchResults results
    ) =>
        new(
            results.TopMatches
                .Select(SkillSearchMatchDto.FromSearchResult)
                .ToList(),
            results.DistinctSources
                .Select(SkillSearchMatchDto.FromSearchResult)
                .ToList()
        );
}

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
