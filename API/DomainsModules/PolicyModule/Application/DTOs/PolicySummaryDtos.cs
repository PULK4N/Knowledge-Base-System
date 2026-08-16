using PolicyModule.Domain.Models;
using PolicyModule.Persistence.Interfaces;

namespace PolicyModule.Application.DTOs;

public sealed record PolicyProjectSummaryDto(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
)
{
    public static PolicyProjectSummaryDto FromReadModel(
        PolicyProjectSummary project
    ) =>
        new(
            project.ProjectId,
            project.ProjectName,
            project.RepositoryPaths.ToList()
        );
}

public sealed record PolicyTopicSummaryDto(
    string TopicName,
    string Description,
    int PolicyCount
)
{
    public static PolicyTopicSummaryDto FromModel(Topic topic) =>
        new(
            topic.TopicName.Name,
            topic.Description,
            topic.Policies.Count
        );
}
