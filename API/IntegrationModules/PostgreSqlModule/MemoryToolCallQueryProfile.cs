using System.Linq.Expressions;
using ActionModule.Persistence;
using Microsoft.EntityFrameworkCore;
using ActionModule.Shared.Models;
using EmbeddingModule;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace PostgreSqlModule;

/// <summary>
/// Query profile for tool calls. It is created per request because relevance
/// ordering ranks the rows against the same keywords the filters matched.
/// </summary>
internal sealed class MemoryToolCallQueryProfile(
    MemoryToolCallFilters requestFilters
)
    : IEntityQueryProfile<
        MemoryToolCallEntry,
        MemoryToolCallFilters,
        MemoryToolCallSortField,
        MemoryToolCall
    >
{
    private const string TextSearchConfiguration = "simple";

    public IQueryable<MemoryToolCallEntry> ApplyFilters(
        IQueryable<MemoryToolCallEntry> query,
        MemoryToolCallFilters filters
    )
    {
        if (filters.ToolName is not null)
        {
            var normalizedToolName = filters.ToolName.ToLowerInvariant();

            query = query.Where(
                toolCall =>
                    EF.Functions.ILike(
                        toolCall.ToolName,
                        "%" + normalizedToolName + "%"
                    )
            );
        }

        if (filters.MemoryAggregateId is { } memoryAggregateId)
        {
            var memoryId = memoryAggregateId.Value;
            query = query.Where(
                toolCall => toolCall.MemoryAggregateId == memoryId
            );
        }

        if (filters.ThreadId is { } threadIdentifier)
        {
            var threadId = threadIdentifier.Value;
            query = query.Where(toolCall => toolCall.ThreadId == threadId);
        }

        if (filters.PromptId is { } promptIdentifier)
        {
            var promptId = promptIdentifier.Value;
            query = query.Where(toolCall => toolCall.PromptId == promptId);
        }

        if (ToTextQuery(filters.InputKeywords) is { } inputQuery)
        {
            query = query.Where(
                toolCall => toolCall.InputSearchVector.Matches(
                    EF.Functions.PlainToTsQuery(
                        TextSearchConfiguration,
                        inputQuery
                    )
                )
            );
        }

        if (ToTextQuery(filters.OutputKeywords) is { } outputQuery)
        {
            query = query.Where(
                toolCall => toolCall.OutputSearchVector.Matches(
                    EF.Functions.PlainToTsQuery(
                        TextSearchConfiguration,
                        outputQuery
                    )
                )
            );
        }

        return query;
    }

    public IQueryable<MemoryToolCallEntry> ApplySearch(
        IQueryable<MemoryToolCallEntry> query,
        string? search
    )
    {
        if (search is null)
            return query;

        var normalizedSearch = search.ToLowerInvariant();

        return query.Where(
            toolCall =>
                EF.Functions.ILike(
                    toolCall.Description,
                    "%" + normalizedSearch + "%"
                )
                || EF.Functions.ILike(
                    toolCall.PayloadJson,
                    "%" + normalizedSearch + "%"
                )
        );
    }

    public IOrderedQueryable<MemoryToolCallEntry> ApplySort(
        IQueryable<MemoryToolCallEntry> query,
        SortRequest<MemoryToolCallSortField> sort
    ) =>
        (sort.Field, sort.Direction) switch
        {
            (
                MemoryToolCallSortField.Timestamp,
                SortDirection.Ascending
            ) => query
                .OrderBy(toolCall => toolCall.Timestamp)
                .ThenBy(toolCall => toolCall.MemoryAggregateId)
                .ThenBy(toolCall => toolCall.PromptId)
                .ThenBy(toolCall => toolCall.ToolCallIndex),
            (
                MemoryToolCallSortField.Timestamp,
                SortDirection.Descending
            ) => query
                .OrderByDescending(toolCall => toolCall.Timestamp)
                .ThenBy(toolCall => toolCall.MemoryAggregateId)
                .ThenBy(toolCall => toolCall.PromptId)
                .ThenBy(toolCall => toolCall.ToolCallIndex),
            (
                MemoryToolCallSortField.ToolName,
                SortDirection.Ascending
            ) => query
                .OrderBy(toolCall => toolCall.ToolName)
                .ThenByDescending(toolCall => toolCall.Timestamp)
                .ThenBy(toolCall => toolCall.MemoryAggregateId)
                .ThenBy(toolCall => toolCall.PromptId)
                .ThenBy(toolCall => toolCall.ToolCallIndex),
            (
                MemoryToolCallSortField.ToolName,
                SortDirection.Descending
            ) => query
                .OrderByDescending(toolCall => toolCall.ToolName)
                .ThenByDescending(toolCall => toolCall.Timestamp)
                .ThenBy(toolCall => toolCall.MemoryAggregateId)
                .ThenBy(toolCall => toolCall.PromptId)
                .ThenBy(toolCall => toolCall.ToolCallIndex),
            (MemoryToolCallSortField.Relevance, _) =>
                ApplyRelevanceSort(query, sort.Direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

    /// <summary>
    /// Ranks by cover density of the input and output keywords, adding both
    /// ranks when both sides are searched. Without keywords there is nothing
    /// to rank, so the order falls back to the timestamp.
    /// </summary>
    private IOrderedQueryable<MemoryToolCallEntry> ApplyRelevanceSort(
        IQueryable<MemoryToolCallEntry> query,
        SortDirection direction
    )
    {
        var inputQuery = ToTextQuery(requestFilters.InputKeywords);
        var outputQuery = ToTextQuery(requestFilters.OutputKeywords);

        Expression<Func<MemoryToolCallEntry, float>>? rank =
            (inputQuery, outputQuery) switch
            {
                ({ } input, { } output) => toolCall =>
                    toolCall.InputSearchVector.RankCoverDensity(
                        EF.Functions.PlainToTsQuery(
                            TextSearchConfiguration,
                            input
                        )
                    )
                    + toolCall.OutputSearchVector.RankCoverDensity(
                        EF.Functions.PlainToTsQuery(
                            TextSearchConfiguration,
                            output
                        )
                    ),
                ({ } input, null) => toolCall =>
                    toolCall.InputSearchVector.RankCoverDensity(
                        EF.Functions.PlainToTsQuery(
                            TextSearchConfiguration,
                            input
                        )
                    ),
                (null, { } output) => toolCall =>
                    toolCall.OutputSearchVector.RankCoverDensity(
                        EF.Functions.PlainToTsQuery(
                            TextSearchConfiguration,
                            output
                        )
                    ),
                _ => null
            };

        if (rank is null)
            return ApplySort(
                query,
                new SortRequest<MemoryToolCallSortField>(
                    MemoryToolCallSortField.Timestamp,
                    direction
                )
            );

        var ranked = direction == SortDirection.Ascending
            ? query.OrderBy(rank)
            : query.OrderByDescending(rank);

        return ranked
            .ThenByDescending(toolCall => toolCall.Timestamp)
            .ThenBy(toolCall => toolCall.MemoryAggregateId)
            .ThenBy(toolCall => toolCall.PromptId)
            .ThenBy(toolCall => toolCall.ToolCallIndex);
    }

    private static string? ToTextQuery(List<string>? keywords) =>
        keywords is null || keywords.Count == 0
            ? null
            : SearchKeywordLimits.ToTextQuery(keywords);

    public Expression<Func<MemoryToolCallEntry, MemoryToolCall>> Projection =>
        toolCall => new MemoryToolCall(
            AggregateId.FromDatabaseGuid(toolCall.MemoryAggregateId),
            new ThreadId(toolCall.ThreadId),
            new PromptId(toolCall.PromptId),
            toolCall.ToolCallIndex,
            toolCall.Timestamp,
            toolCall.ToolName,
            toolCall.ToolUseId,
            toolCall.Description,
            toolCall.PayloadJson
        );
}
