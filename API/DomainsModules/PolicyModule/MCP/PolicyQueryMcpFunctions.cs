using Microsoft.Extensions.AI;
using PolicyModule.Application.Models;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class PolicyQueryMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
            (Func<IServiceProvider, string, string?, Task<GetPoliciesByRepositoryResult>>)GetByRepository,
            "policy_get_by_repository",
            "Gets the complete policy context for an absolute repository path. Pass agentFamily, such as 'claude' or 'codex', to also receive the policies that apply only to that agent family. If status is RepositoryMappingRequired, stop reasoning, show the listed projects and repository paths, and ask the user to select a project or provide a unique new project name. Then add/create the mapping and retry this tool. Never select or create a project without the user's answer."
        )
        ];

    private static Task<GetPoliciesByRepositoryResult> GetByRepository(
        IServiceProvider services,
        string repositoryPath,
        string? agentFamily = null
    ) =>
        PolicyMcpActionExecutor.ExecuteQuery<
            GetPoliciesByRepositoryQuery,
            GetPoliciesByRepositoryResult
        >(
            services,
            query =>
            {
                query.RepositoryPath = repositoryPath;
                query.AgentFamily = agentFamily;
            }
        );
}
