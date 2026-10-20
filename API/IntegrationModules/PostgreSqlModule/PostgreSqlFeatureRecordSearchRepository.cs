using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlFeatureRecordSearchRepository(
    EventSourcingDbContext dbContext
) : IFeatureRecordSearchRepository
{
    private const string Table = "FeatureRecordSearchEntries";
    private const string SourceColumns = "\"FeatureAggregateId\"";
    private const string TieBreakers = "\"UpdatedAt\" DESC, \"RecordId\", \"ChunkIndex\"";

    public async Task Write(
        List<AggregateId> featureAggregateIds,
        List<FeatureRecordSearchDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateIds = featureAggregateIds
            .Select(aggregateId => aggregateId.Value)
            .Distinct()
            .ToList();

        if (aggregateIds.Count == 0)
            return;

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Set<FeatureRecordSearchEntry>()
            .Where(entry => aggregateIds.Contains(entry.FeatureAggregateId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<FeatureRecordSearchEntry>().AddRangeAsync(
            documents.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<FeatureRecordSearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(keywords, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<FeatureRecordSearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount
    ) =>
        dbContext.Set<FeatureRecordSearchEntry>()
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
            .ThenByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.FeatureAggregateId)
            .ThenBy(entry => entry.RecordId)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);

    public async Task<List<FeatureRecordSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorQuery(embedding, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<FeatureRecordSearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount
    )
    {
        var vector = new Vector(embedding.ToArray());

        return dbContext.Set<FeatureRecordSearchEntry>()
            .AsNoTracking()
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.FeatureAggregateId)
            .ThenBy(entry => entry.RecordId)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);
    }

    public async Task<List<FeatureRecordSearchCandidate>> SearchTextBySource(
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

    internal IQueryable<FeatureRecordSearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<FeatureRecordSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(Table, SourceColumns, TieBreakers),
                SourceSearchSql.TextParameters(
                    keywords,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    public async Task<List<FeatureRecordSearchCandidate>> SearchVectorBySource(
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

    internal IQueryable<FeatureRecordSearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<FeatureRecordSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(Table, SourceColumns, TieBreakers),
                SourceSearchSql.VectorParameters(
                    embedding,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    private static FeatureRecordSearchEntry ToEntry(
        FeatureRecordSearchDocument document
    ) =>
        new()
        {
            FeatureAggregateId = document.FeatureAggregateId.Value,
            FeatureName = document.FeatureName,
            RecordId = document.RecordId,
            RecordType = document.RecordType,
            Title = document.Title,
            SourceType = document.SourceType,
            SourceReference = document.SourceReference,
            UpdatedAt = document.UpdatedAt,
            ChunkIndex = document.ChunkIndex,
            Text = document.Text,
            Embedding = new Vector(document.Embedding.ToArray())
        };

    private static FeatureRecordSearchCandidate ToCandidate(
        FeatureRecordSearchEntry entry
    ) =>
        new(
            AggregateId.FromDatabaseGuid(entry.FeatureAggregateId),
            entry.FeatureName,
            entry.RecordId,
            entry.RecordType,
            entry.Title,
            entry.SourceType,
            entry.SourceReference,
            entry.UpdatedAt,
            entry.ChunkIndex,
            entry.Text
        );
}
