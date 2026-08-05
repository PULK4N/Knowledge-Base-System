using Microsoft.Extensions.AI;
using PolicyModule.Application.Queries;

namespace PolicyModule.MCP;

internal static class PolicyQueryMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            PolicyMcpFunctions.Create(
            GetByRepository,
            "policy_get_by_repository",
            "Gets the complete Markdown policy context for an absolute repository path, combining general, project, and associated-topic policies."
        )
        ];

    private static Task<string> GetByRepository(IServiceProvider services, string repositoryPath) =>
        PolicyMcpActionExecutor.ExecuteQuery<GetPoliciesByRepositoryQuery, string>(
            services,
            query => query.RepositoryPath = repositoryPath
        );
}
