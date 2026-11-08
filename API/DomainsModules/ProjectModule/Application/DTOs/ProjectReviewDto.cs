using ProjectModule.Shared;
using ProjectModule.Domain;
using ProjectModule.Domain.Models;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application.DTOs;

/// <summary>
/// What a review script needs: the level to model mapping and the skills used
/// for review, in link order. The script resolves each skill's model itself.
/// </summary>
public sealed record ProjectReviewDto(
    Guid ProjectId,
    Dictionary<ReviewLevel, ReviewModel> LevelModels,
    List<ProjectReviewSkillDto> ReviewSkills
)
{
    public static ProjectReviewDto FromStateData(
        ProjectStateData state,
        List<SkillUseScenarios> skills
    )
    {
        var skillNames = skills.ToDictionary(
            skill => skill.SkillId,
            skill => skill.Name
        );

        return new(
            state.Id.Value,
            state.ReviewLevelModels.ToDictionary(),
            state.RelatedSkillIds
                .Where(
                    skillId => state.SkillReviews.TryGetValue(skillId, out var review)
                        && review.UsedForReview
                )
                .Select(
                    skillId =>
                    {
                        var review = state.SkillReviews[skillId];
                        return new ProjectReviewSkillDto(
                            skillId.Value,
                            skillNames.GetValueOrDefault(skillId.Value, string.Empty),
                            review.ReviewLevel,
                            review.ReviewNotes
                        );
                    }
                )
                .ToList()
        );
    }
}

public sealed record ProjectReviewSkillDto(
    Guid SkillId,
    string SkillName,
    ReviewLevel? ReviewLevel,
    string ReviewNotes
);
