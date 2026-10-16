using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlMemorySearchRepository(
    EventSourcingDbContext dbContext
) : IMemorySearchRepository
{
    private const string Table = "MemorySearchEntries";
    private const string SourceColumns = "\"MemoryAggregateId\"";
    private const string TieBreakers = "\"PromptStartTimestamp\" DESC, \"PromptId\", \"HookIndex\", \"ChunkIndex\"";
    private const string FromParameterName = "from";
    private const string ToParameterName = "to";
    private const string StrictIterativeScanSql =
        "SET LOCAL hnsw.iterative_scan = strict_order";

    public async Task Write(
        IReadOnlyCollection<AggregateId> memoryAggregateIds,
        IReadOnlyCollection<MemorySearchDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateIds = memoryAggregateIds
            .Select(aggregateId => aggregateId.Value)
            .Distinct()
            .ToList();

        if (aggregateIds.Count == 0)
            return;

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Set<MemorySearchEntry>()
            .Where(entry => aggregateIds.Contains(entry.MemoryAggregateId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<MemorySearchEntry>().AddRangeAsync(
            documents.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(
            keywords,
            candidateCount,
            timeRange
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null
    )
    {
        return FilterByTime(
                dbContext.Set<MemorySearchEntry>().AsNoTracking(),
                timeRange
            )
            .Where(
                entry => entry.SearchVector.Matches(
                    EF.Functions.PlainToTsQuery(
                        "simple",
                        SearchKeywordLimits.ToTextQuery(keywords)
                    )
                )
            )
            .OrderByDescending(
                entry => entry.SearchVector.RankCoverDensity(
                    EF.Functions.PlainToTsQuery(
                        "simple",
                        SearchKeywordLimits.ToTextQuery(keywords)
                    )
                )
            )
            .ThenByDescending(entry => entry.PromptStartTimestamp)
            .Take(candidateCount);
    }

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await ReadVectorEntries(
            CreateVectorQuery(embedding, candidateCount, timeRange),
            timeRange,
            cancellationToken
        );

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null
    )
    {
        var vector = new Vector(embedding.ToArray());

        return FilterByTime(
                dbContext.Set<MemorySearchEntry>().AsNoTracking(),
                timeRange
            )
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.PromptStartTimestamp)
            .Take(candidateCount);
    }

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextSourceQuery(
            keywords,
            sourceCount,
            candidateCount,
            timeRange
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(
                    Table,
                    SourceColumns,
                    TieBreakers,
                    TimeFilterSql(timeRange)
                ),
                [
                    .. SourceSearchSql.TextParameters(
                        keywords,
                        sourceCount,
                        candidateCount
                    ),
                    .. TimeFilterParameters(timeRange)
                ]
            )
            .AsNoTracking();

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await ReadVectorEntries(
            CreateVectorSourceQuery(
                embedding,
                sourceCount,
                candidateCount,
                timeRange
            ),
            timeRange,
            cancellationToken
        );

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        MemorySearchTimeRange? timeRange = null
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(
                    Table,
                    SourceColumns,
                    TieBreakers,
                    TimeFilterSql(timeRange)
                ),
                [
                    .. SourceSearchSql.VectorParameters(
                        embedding,
                        sourceCount,
                        candidateCount
                    ),
                    .. TimeFilterParameters(timeRange)
                ]
            )
            .AsNoTracking();

    private static IQueryable<MemorySearchEntry> FilterByTime(
        IQueryable<MemorySearchEntry> query,
        MemorySearchTimeRange? timeRange
    )
    {
        if (timeRange?.From is { } from)
            query = query.Where(entry => entry.PromptStartTimestamp >= from);
        if (timeRange?.To is { } to)
            query = query.Where(entry => entry.PromptStartTimestamp <= to);

        return query;
    }

    private static string? TimeFilterSql(MemorySearchTimeRange? timeRange)
    {
        var conditions = new List<string>();
        if (timeRange?.From is not null)
            conditions.Add($"\"PromptStartTimestamp\" >= @{FromParameterName}");
        if (timeRange?.To is not null)
            conditions.Add($"\"PromptStartTimestamp\" <= @{ToParameterName}");

        return conditions.Count == 0
            ? null
            : string.Join(" AND ", conditions);
    }

    private static List<NpgsqlParameter> TimeFilterParameters(
        MemorySearchTimeRange? timeRange
    )
    {
        var parameters = new List<NpgsqlParameter>();
        if (timeRange?.From is { } from)
        {
            parameters.Add(
                new NpgsqlParameter(FromParameterName, NpgsqlDbType.TimestampTz)
                {
                    Value = from
                }
            );
        }
        if (timeRange?.To is { } to)
        {
            parameters.Add(
                new NpgsqlParameter(ToParameterName, NpgsqlDbType.TimestampTz)
                {
                    Value = to
                }
            );
        }

        return parameters;
    }

    /// <summary>
    /// An HNSW scan filters rows only after it has collected its candidate
    /// window, so a time filter could leave fewer matches than requested.
    /// pgvector's strict iterative scan keeps reading the index in exact order
    /// until the filtered window is full. SET LOCAL needs a transaction; inside
    /// a caller's transaction the setting lasts until that transaction ends.
    /// </summary>
    private async Task<List<MemorySearchEntry>> ReadVectorEntries(
        IQueryable<MemorySearchEntry> query,
        MemorySearchTimeRange? timeRange,
        CancellationToken cancellationToken
    )
    {
        if (timeRange is null || timeRange.IsEmpty)
            return await query.ToListAsync(cancellationToken);

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Database.ExecuteSqlRawAsync(
            StrictIterativeScanSql,
            cancellationToken
        );
        var entries = await query.ToListAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);

        return entries;
    }

    private static MemorySearchEntry ToEntry(
        MemorySearchDocument document
    ) =>
        new()
        {
            MemoryAggregateId = document.MemoryAggregateId.Value,
            ThreadId = document.ThreadId.Value,
            PromptId = document.PromptId.Value,
            HookIndex = document.HookIndex,
            ChunkIndex = document.ChunkIndex,
            PromptStartTimestamp = document.PromptStartTimestamp,
            HookEventName = document.HookEventName,
            Text = document.Text,
            Embedding = new Vector(document.Embedding.ToArray())
        };

    private static MemorySearchCandidate ToCandidate(
        MemorySearchEntry entry
    ) =>
        new(
            AggregateId.FromDatabaseGuid(entry.MemoryAggregateId),
            new MemoryModule.Domain.Models.ThreadId(entry.ThreadId),
            new MemoryModule.Domain.Models.PromptId(entry.PromptId),
            entry.HookIndex,
            entry.ChunkIndex,
            entry.PromptStartTimestamp,
            entry.HookEventName,
            entry.Text
        );
}
