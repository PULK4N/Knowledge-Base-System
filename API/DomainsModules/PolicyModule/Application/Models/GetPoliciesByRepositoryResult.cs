namespace PolicyModule.Application.Models;

public sealed record GetPoliciesByRepositoryResult(
    string Status,
    string RepositoryPath,
    string? Policies,
    bool RequiresUserInput,
    string? Message,
    List<ProjectRepositoryOption> Projects
)
{
    public const string OkStatus = "OK";
    public const string RepositoryMappingRequiredStatus =
        "RepositoryMappingRequired";
    public const string AgentFamilyNotFoundStatus = "AgentFamilyNotFound";

    public static GetPoliciesByRepositoryResult Found(
        string repositoryPath,
        string policies
    ) =>
        new(
            OkStatus,
            repositoryPath,
            policies,
            false,
            null,
            []
        );

    /// <summary>
    /// The caller asked for an agent family that has not been created yet.
    /// Returned as HTTP 400 so a plugin hook can create the family and retry.
    /// </summary>
    public static GetPoliciesByRepositoryResult AgentFamilyNotFound(
        string repositoryPath,
        string agentFamily
    ) =>
        new(
            AgentFamilyNotFoundStatus,
            repositoryPath,
            null,
            false,
            $"Agent family '{agentFamily}' does not exist. "
                + "Create it and retry the request.",
            []
        );

    public static GetPoliciesByRepositoryResult MappingRequired(
        string repositoryPath,
        List<ProjectRepositoryOption> projects
    ) =>
        new(
            RepositoryMappingRequiredStatus,
            repositoryPath,
            null,
            true,
            $"Repository '{repositoryPath}' is not mapped to a policy project. "
                + "Stop and ask the user whether it belongs to one of the listed projects, "
                + "or ask for a unique name for a new project. After adding the repository "
                + "to the selected project or creating the new project, retry policy_get_by_repository.",
            projects
        );
}

public sealed record ProjectRepositoryOption(
    Guid ProjectId,
    string ProjectName,
    List<string> RepositoryPaths
);
