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
    private const string ModelParameterName = "model";
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
        MemorySearchFilter? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(
            keywords,
            candidateCount,
            filter
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount,
        MemorySearchFilter? filter = null
    )
    {
        return Filter(
                dbContext.Set<MemorySearchEntry>().AsNoTracking(),
                filter
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
        MemorySearchFilter? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await ReadVectorEntries(
            CreateVectorQuery(embedding, candidateCount, filter),
            filter,
            cancellationToken
        );

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount,
        MemorySearchFilter? filter = null
    )
    {
        var vector = new Vector(embedding.ToArray());

        return Filter(
                dbContext.Set<MemorySearchEntry>().AsNoTracking(),
                filter
            )
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.PromptStartTimestamp)
            .Take(candidateCount);
    }

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        MemorySearchFilter? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextSourceQuery(
            keywords,
            sourceCount,
            candidateCount,
            filter
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        MemorySearchFilter? filter = null
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(
                    Table,
                    SourceColumns,
                    TieBreakers,
                    FilterSql(filter)
                ),
                [
                    .. SourceSearchSql.TextParameters(
                        keywords,
                        sourceCount,
                        candidateCount
                    ),
                    .. FilterParameters(filter)
                ]
            )
            .AsNoTracking();

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        MemorySearchFilter? filter = null,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await ReadVectorEntries(
            CreateVectorSourceQuery(
                embedding,
                sourceCount,
                candidateCount,
                filter
            ),
            filter,
            cancellationToken
        );

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        MemorySearchFilter? filter = null
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(
                    Table,
                    SourceColumns,
                    TieBreakers,
                    FilterSql(filter)
                ),
                [
                    .. SourceSearchSql.VectorParameters(
                        embedding,
                        sourceCount,
                        candidateCount
                    ),
                    .. FilterParameters(filter)
                ]
            )
            .AsNoTracking();

    private static IQueryable<MemorySearchEntry> Filter(
        IQueryable<MemorySearchEntry> query,
        MemorySearchFilter? filter
    )
    {
        if (filter?.TimeRange?.From is { } from)
            query = query.Where(entry => entry.PromptStartTimestamp >= from);
        if (filter?.TimeRange?.To is { } to)
            query = query.Where(entry => entry.PromptStartTimestamp <= to);
        if (ModelOf(filter) is { } model)
            query = query.Where(entry => entry.Model == model);

        return query;
    }

    private static string? FilterSql(MemorySearchFilter? filter)
    {
        var conditions = new List<string>();
        if (filter?.TimeRange?.From is not null)
            conditions.Add($"\"PromptStartTimestamp\" >= @{FromParameterName}");
        if (filter?.TimeRange?.To is not null)
            conditions.Add($"\"PromptStartTimestamp\" <= @{ToParameterName}");
        if (ModelOf(filter) is not null)
            conditions.Add($"\"Model\" = @{ModelParameterName}");

        return conditions.Count == 0
            ? null
            : string.Join(" AND ", conditions);
    }

    private static List<NpgsqlParameter> FilterParameters(
        MemorySearchFilter? filter
    )
    {
        var parameters = new List<NpgsqlParameter>();
        if (filter?.TimeRange?.From is { } from)
        {
            parameters.Add(
                new NpgsqlParameter(FromParameterName, NpgsqlDbType.TimestampTz)
                {
                    Value = from
                }
            );
        }
        if (filter?.TimeRange?.To is { } to)
        {
            parameters.Add(
                new NpgsqlParameter(ToParameterName, NpgsqlDbType.TimestampTz)
                {
                    Value = to
                }
            );
        }
        if (ModelOf(filter) is { } model)
        {
            parameters.Add(
                new NpgsqlParameter(ModelParameterName, NpgsqlDbType.Text)
                {
                    Value = model
                }
            );
        }

        return parameters;
    }

    private static string? ModelOf(MemorySearchFilter? filter) =>
        filter?.Model?.Trim() is { Length: > 0 } model ? model : null;

    /// <summary>
    /// An HNSW scan filters rows only after it has collected its candidate
    /// window, so a time or model filter could leave fewer matches than
    /// requested.
    /// pgvector's strict iterative scan keeps reading the index in exact order
    /// until the filtered window is full. SET LOCAL needs a transaction; inside
    /// a caller's transaction the setting lasts until that transaction ends.
    /// </summary>
    private async Task<List<MemorySearchEntry>> ReadVectorEntries(
        IQueryable<MemorySearchEntry> query,
        MemorySearchFilter? filter,
        CancellationToken cancellationToken
    )
    {
        if (filter is null || filter.IsEmpty)
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
            Model = document.Model,
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
            entry.Model,
            entry.Text
        );
}
