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
                    int,
                    Task<MemorySearchQueryResult>
                >)Search,
                "memory_search",
                "Searches chat memories using hybrid semantic vector and full-text ranking. Returns distinct relevant sessions within a bounded token budget."
            ),
            CreateFunction(
                (Func<
                    IServiceProvider,
                    Guid,
                    string,
                    Task<MemoryCommandResult>
                >)AddSummary,
                "memory_summary_add",
                "Adds or replaces the summary for an existing chat memory identified by its Codex thread ID."
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
        string query,
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
                search.MaxTokens = maxTokens;
            }
        );

    private static Task<MemoryCommandResult> AddSummary(
        IServiceProvider services,
        Guid threadId,
        string summary
    ) =>
        MemoryMcpActionExecutor.ExecuteCommand<AddChatSummaryCommand, MemoryCommandResult>(
            services,
            command =>
            {
                command.ThreadId = new ThreadId(threadId);
                command.Summary = summary;
            }
        );
}
