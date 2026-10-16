using System.ComponentModel;
using ActionModule.Shared.Models;
using MemoryModule.Application.Commands;
using MemoryModule.Application.DTOs;
using MemoryModule.Application.Models;
using MemoryModule.Application.Queries;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.Extensions.AI;

namespace MemoryModule.MCP;

public static class MemoryMcpFunctions
{
    private const int TimeSearchPageSize = 10;
    private const string FromDescription =
        "Start of the range as an ISO 8601 timestamp, for example 2026-10-16T08:00:00Z. A timestamp without a zone is read as UTC.";
    private const string ToDescription =
        "End of the range as an ISO 8601 timestamp, for example 2026-10-16T18:00:00Z. A timestamp without a zone is read as UTC.";

    public static List<AIFunction> Create() =>
        [
            CreateFunction(
                (Func<
                    IServiceProvider,
                    string,
                    List<string>,
                    int,
                    DateTime?,
                    DateTime?,
                    Task<MemorySearchQueryResult>
                >)Search,
                "memory_search",
                "Searches chat memories. Two separate inputs: query is a meaningful sentence and drives semantic vector matching, while keywords are the words a memory must all contain for full-text matching. Optional from and to limit matches to text written in that time range. Returns distinct relevant sessions within a bounded token budget."
            ),
            CreateFunction(
                (Func<
                    IServiceProvider,
                    DateTime?,
                    DateTime?,
                    int,
                    int,
                    Task<PagedResult<MemorySummaryDto>>
                >)SearchByTime,
                "memory_search_by_time",
                "Finds chat memories that were active within a time range, newest activity first. A memory matches when its chat overlaps the range: it ended at or after 'from' and started at or before 'to'. Omit either bound to leave that side open. Returns each memory's ID, thread ID, title, summary, prompt count, and timestamps."
            ),
            CreateFunction(
                (Func<
                    IServiceProvider,
                    Guid,
                    int,
                    int,
                    Task<MemoryConversationTextResult>
                >)GetConversationText,
                "memory_conversation_get",
                "Gets the chat text of one memory in conversation order, without raw hook payloads. Returns at most 'limit' messages starting at 'offset', plus the total message count and whether more messages follow. Page through a long chat by increasing offset."
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
        int maxTokens = SearchMemoryQuery.DefaultMaximumTokens,
        [Description(FromDescription)]
        DateTime? from = null,
        [Description(ToDescription)]
        DateTime? to = null
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
                search.From = from;
                search.To = to;
            }
        );

    private static Task<PagedResult<MemorySummaryDto>> SearchByTime(
        IServiceProvider services,
        [Description(FromDescription)]
        DateTime? from = null,
        [Description(ToDescription)]
        DateTime? to = null,
        int page = Pagination.DefaultPage,
        [Description("Memories per page, from 1 to 100.")]
        int pageSize = TimeSearchPageSize
    ) =>
        MemoryMcpActionExecutor.ExecuteQuery<
            SearchMemoriesQuery,
            PagedResult<MemorySummaryDto>
        >(
            services,
            search =>
            {
                search.ActiveFrom = from;
                search.ActiveTo = to;
                search.Page = page;
                search.PageSize = pageSize;
                search.SortBy = MemorySummarySortField.LastActivity;
                search.SortDirection = SortDirection.Descending;
            }
        );

    private static Task<MemoryConversationTextResult> GetConversationText(
        IServiceProvider services,
        [Description("The memory ID returned by memory_search or memory_search_by_time.")]
        Guid memoryId,
        [Description("Maximum number of messages to return, from 1 to 100.")]
        int limit = GetMemoryConversationTextQuery.DefaultLimit,
        [Description("Number of messages to skip from the start of the chat.")]
        int offset = 0
    ) =>
        MemoryMcpActionExecutor.ExecuteQuery<
            GetMemoryConversationTextQuery,
            MemoryConversationTextResult
        >(
            services,
            query =>
            {
                query.MemoryId = memoryId;
                query.Limit = limit;
                query.Offset = offset;
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
