using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Application.DTOs;

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
