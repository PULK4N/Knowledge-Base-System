using ActionModule.Shared;
using System.ComponentModel;
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
                    List<string>,
                    int,
                    Task<KnowledgeSearchResultsDto>
                >)Search,
                new AIFunctionFactoryOptions
                {
                    Name = "knowledge_search",
                    Description = "Searches the complete knowledge base across memories, skills, feature information, plans, research discoveries, and conversation records. Two separate inputs: query is a meaningful sentence and drives semantic vector matching, while keywords are the words a record must all contain for full-text matching. Returns topMatches with the best ranked chunks, which may repeat one source, and distinctSources with the best chunk of each different skill, feature, or memory. Results include fluid source metadata and available timestamps."
                }
            )
        ];

    private static async Task<KnowledgeSearchResultsDto> Search(
        IServiceProvider services,
        [Description("A meaningful sentence describing what you need. Only the semantic vector match reads it.")]
        [StringLength(
            SearchKnowledgeQuery.MaximumSearchTextLength,
            MinimumLength = 1
        )]
        string query,
        [Description("Words that must all appear in a matching record. Every word is required, so pass only the distinctive terms, and include common words only when the exact wording matters.")]
        List<string> keywords,
        int resultCount = SearchKnowledgeQuery.DefaultResultCount
    )
    {
        var search = services.GetRequiredService<SearchKnowledgeQuery>();
        search.SearchText = query;
        search.Keywords = keywords;
        search.ResultCount = resultCount;
        var executor = await services
            .GetRequiredService<IExecutorProvider>()
            .GetExecutor();

        return await search.Execute(executor);
    }
}
