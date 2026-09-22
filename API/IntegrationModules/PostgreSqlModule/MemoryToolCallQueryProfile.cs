using System.Linq.Expressions;
using ActionModule.Persistence;
using Microsoft.EntityFrameworkCore;
using ActionModule.Shared.Models;
using EventSourcing.Shared.Models;
using MemoryModule.Domain.Models;
using MemoryModule.Persistence.Interfaces;

namespace PostgreSqlModule;

internal sealed class MemoryToolCallQueryProfile
    : IEntityQueryProfile<
        MemoryToolCallEntry,
        MemoryToolCallFilters,
        MemoryToolCallSortField,
        MemoryToolCall
    >
{
    public IQueryable<MemoryToolCallEntry> ApplyFilters(
        IQueryable<MemoryToolCallEntry> query,
        MemoryToolCallFilters filters
    )
    {
        if (filters.ToolName is null)
            return query;

        var normalizedToolName = filters.ToolName.ToLowerInvariant();

        return query.Where(
            toolCall =>
                EF.Functions.ILike(
                    toolCall.ToolName,
                    "%" + normalizedToolName + "%"
                )
        );
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
            _ => throw new ArgumentOutOfRangeException(nameof(sort))
        };

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
