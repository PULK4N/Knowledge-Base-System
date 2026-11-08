using ProjectModule.Domain;
using ProjectModule.Domain.Models;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application.DTOs;

public sealed record PolicyProjectDetailsDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    List<string> RepositoryPaths,
    List<string> TopicNames,
    List<ProjectSkillDto> Skills,
    Dictionary<ReviewLevel, ReviewModel> ReviewLevelModels
)
{
    public static PolicyProjectDetailsDto FromStateData(
        ProjectStateData state,
        List<SkillUseScenarios> skills
    ) =>
        new(
            state.Id.Value,
            state.ProjectName,
            state.ProjectDescription,
            state.RepositoryPaths.ToList(),
            state.RelatedTopics
                .Select(topic => topic.Name)
                .ToList(),
            state.RelatedSkillIds
                .Join(
                    skills,
                    skillId => skillId.Value,
                    skill => skill.SkillId,
                    (skillId, skill) => ProjectSkillDto.Create(
                        skill,
                        state.SkillReviews.GetValueOrDefault(skillId)
                    )
                )
                .OrderBy(skill => skill.Name, StringComparer.Ordinal)
                .ToList(),
            state.ReviewLevelModels.ToDictionary()
        );
}

public sealed record ProjectSkillDto(
    Guid SkillId,
    string Name,
    List<string> UseScenarios,
    bool UsedForReview,
    string ReviewNotes,
    ReviewLevel? ReviewLevel
)
{
    public static ProjectSkillDto Create(
        SkillUseScenarios skill,
        ProjectSkillReview? review
    ) =>
        new(
            skill.SkillId,
            skill.Name,
            skill.UseScenarios,
            review?.UsedForReview ?? false,
            review?.ReviewNotes ?? string.Empty,
            review?.ReviewLevel
        );
}
