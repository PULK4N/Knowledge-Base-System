using ProjectModule.Domain;

namespace ProjectModule.Application.DTOs;

public sealed record PolicyProjectDetailsDto(
    Guid ProjectId,
    string ProjectName,
    string ProjectDescription,
    List<string> RepositoryPaths,
    List<string> TopicNames
)
{
    public static PolicyProjectDetailsDto FromStateData(
        ProjectStateData state
    ) =>
        new(
            state.Id.Value,
            state.ProjectName,
            state.ProjectDescription,
            state.RepositoryPaths.ToList(),
            state.RelatedTopics
                .Select(topic => topic.Name)
                .ToList()
        );
}
