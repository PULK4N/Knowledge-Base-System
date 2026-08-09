using Microsoft.Extensions.AI;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class PolicyQueryMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
            GetByRepository,
            "policy_get_by_repository",
            "Gets the complete policy context for an absolute repository path. If status is RepositoryMappingRequired, stop reasoning, show the listed projects and repository paths, and ask the user to select a project or provide a unique new project name. Then add/create the mapping and retry this tool. Never select or create a project without the user's answer."
        )
        ];

    private static Task<GetPoliciesByRepositoryResult> GetByRepository(
        IServiceProvider services,
        string repositoryPath
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            GetPoliciesByRepositoryQuery,
            GetPoliciesByRepositoryResult
        >(
            services,
            query => query.RepositoryPath = repositoryPath
        );
}
