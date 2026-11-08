using ProjectModule.Domain;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application.DTOs;

public sealed record PolicyProjectDetailsDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    List<string> RepositoryPaths,
    List<string> TopicNames,
    List<ProjectSkillDto> Skills
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
            skills.Select(ProjectSkillDto.FromReadModel).ToList()
        );
}

public sealed record ProjectSkillDto(
    Guid SkillId,
    string Name,
    List<string> UseScenarios
)
{
    public static ProjectSkillDto FromReadModel(SkillUseScenarios skill) =>
        new(skill.SkillId, skill.Name, skill.UseScenarios);
}
