using System.ComponentModel;
using MemoryModule.Application.Commands;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Models;
using MemoryModule.Application.Queries;
using MemoryModule.Domain.Models;
using Microsoft.Extensions.AI;

namespace MemoryModule.MCP;

public static class MemoryMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            CreateFunction(
                (Func<
                    IServiceProvider,
                    string,
                    List<string>,
                    int,
                    Task<MemorySearchQueryResult>
                >)Search,
                "memory_search",
                "Searches chat memories. Two separate inputs: query is a meaningful sentence and drives semantic vector matching, while keywords are the words a memory must all contain for full-text matching. Returns distinct relevant sessions within a bounded token budget."
            ),
            CreateFunction(
                (Func<
                    IServiceProvider,
                    Guid,
                    string,
                    List<MemoryRelatedEntityDto>?,
                    Task<MemoryCommandResult>
                >)AddSummary,
                "memory_summary_add",
                "Adds or replaces the summary for an existing chat memory identified by its session/thread ID. Write a summary with IDs of entities created or updated during this session. Exclude entities only read, consulted, or used. Related entities accumulate across summaries as a deduplicated set; an omitted or empty list preserves existing references."
            )
        ];

    private static AIFunction CreateFunction(
        Delegate method,
        string name,
        string description
    ) =>
        AIFunctionFactory.Create(
            method,
            new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            }
        );

    private static Task<MemorySearchQueryResult> Search(
        IServiceProvider services,
        [Description("A meaningful sentence describing what you need. Only the semantic vector match reads it.")]
        string query,
        [Description("Words that must all appear in a matching record. Every word is required, so pass only the distinctive terms, and include common words only when the exact wording matters.")]
        List<string> keywords,
        int maxTokens = SearchMemoryQuery.DefaultMaximumTokens
    ) =>
        MemoryMcpActionExecutor.ExecuteQuery<
            SearchMemoryQuery,
            MemorySearchQueryResult
        >(
            services,
            search =>
            {
                search.SearchText = query;
                search.Keywords = keywords;
                search.MaxTokens = maxTokens;
            }
        );

    private static Task<MemoryCommandResult> AddSummary(
        IServiceProvider services,
        Guid threadId,
        string summary,
        [Description("Entities created or updated in this session, each with its type and ID; exclude entities only read, consulted, or used.")]
        List<MemoryRelatedEntityDto>? relatedEntities = null
    ) =>
        MemoryMcpActionExecutor.ExecuteCommand<AddChatSummaryCommand, MemoryCommandResult>(
            services,
            command =>
            {
                command.ThreadId = new ThreadId(threadId);
                command.Summary = summary;
                command.RelatedEntities = relatedEntities ?? [];
            }
        );
}
