using ActionModule.Shared;
using System.ComponentModel.DataAnnotations;
using KnowledgeSearchModule.Application;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeSearchModule.MCP;

public static class KnowledgeSearchMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            AIFunctionFactory.Create(
                (Func<
                    IServiceProvider,
                    string,
                    int,
                    Task<List<KnowledgeSearchMatchDto>>
                >)Search,
                new AIFunctionFactoryOptions
                {
                    Name = "knowledge_search",
                    Description = "Searches the complete knowledge base using hybrid semantic vector and full-text ranking across memories, skills, feature information, plans, research discoveries, and conversation records. Results include fluid source metadata and available timestamps."
                }
            )
        ];

    private static async Task<List<KnowledgeSearchMatchDto>> Search(
        IServiceProvider services,
        [StringLength(
            SearchKnowledgeQuery.MaximumSearchTextLength,
            MinimumLength = 1
        )]
        string query,
        int resultCount = SearchKnowledgeQuery.DefaultResultCount
    )
    {
        var search = services.GetRequiredService<SearchKnowledgeQuery>();
        search.SearchText = query;
        search.ResultCount = resultCount;
        var executor = await services
            .GetRequiredService<IExecutorProvider>()
            .GetExecutor();

        return await search.Execute(executor);
    }
}
