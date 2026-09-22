using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using MemoryModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
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
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(keywords, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount
    )
    {
        return dbContext.Set<MemorySearchEntry>()
            .AsNoTracking()
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
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorQuery(embedding, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount
    )
    {
        var vector = new Vector(embedding.ToArray());

        return dbContext.Set<MemorySearchEntry>()
            .AsNoTracking()
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.PromptStartTimestamp)
            .Take(candidateCount);
    }

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchTextBySource(
        List<string> keywords,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextSourceQuery(
            keywords,
            sourceCount,
            candidateCount
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(Table, SourceColumns, TieBreakers),
                SourceSearchSql.TextParameters(
                    keywords,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    public async Task<IReadOnlyList<MemorySearchCandidate>> SearchVectorBySource(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorSourceQuery(
            embedding,
            sourceCount,
            candidateCount
        ).ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<MemorySearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<MemorySearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(Table, SourceColumns, TieBreakers),
                SourceSearchSql.VectorParameters(
                    embedding,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

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
