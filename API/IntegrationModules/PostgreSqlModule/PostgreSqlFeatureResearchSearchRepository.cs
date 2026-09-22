using System.Collections.Immutable;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using FeatureModule.Persistence.Interfaces;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlFeatureResearchSearchRepository(
    EventSourcingDbContext dbContext
) : IFeatureResearchSearchRepository
{
    private const string Table = "FeatureResearchSearchEntries";
    private const string SourceColumns = "\"FeatureAggregateId\"";
    private const string TieBreakers = "\"UpdatedAt\" DESC, \"ResearchDiscoveryId\", \"ChunkIndex\"";

    public async Task Write(
        List<AggregateId> featureAggregateIds,
        List<FeatureResearchSearchDocument> documents,
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

        await dbContext.Set<FeatureResearchSearchEntry>()
            .Where(entry => aggregateIds.Contains(entry.FeatureAggregateId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<FeatureResearchSearchEntry>().AddRangeAsync(
            documents.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<FeatureResearchSearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(keywords, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<FeatureResearchSearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount
    ) =>
        dbContext.Set<FeatureResearchSearchEntry>()
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
            .ThenBy(entry => entry.ResearchDiscoveryId)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);

    public async Task<List<FeatureResearchSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorQuery(embedding, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<FeatureResearchSearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount
    )
    {
        var vector = new Vector(embedding.ToArray());

        return dbContext.Set<FeatureResearchSearchEntry>()
            .AsNoTracking()
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.FeatureAggregateId)
            .ThenBy(entry => entry.ResearchDiscoveryId)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);
    }

    public async Task<List<FeatureResearchSearchCandidate>> SearchTextBySource(
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

    internal IQueryable<FeatureResearchSearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<FeatureResearchSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(Table, SourceColumns, TieBreakers),
                SourceSearchSql.TextParameters(
                    keywords,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    public async Task<List<FeatureResearchSearchCandidate>> SearchVectorBySource(
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

    internal IQueryable<FeatureResearchSearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<FeatureResearchSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(Table, SourceColumns, TieBreakers),
                SourceSearchSql.VectorParameters(
                    embedding,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    private static FeatureResearchSearchEntry ToEntry(
        FeatureResearchSearchDocument document
    ) =>
        new()
        {
            FeatureAggregateId = document.FeatureAggregateId.Value,
            FeatureName = document.FeatureName,
            ResearchDiscoveryId = document.ResearchDiscoveryId,
            Title = document.Title,
            SourceType = document.SourceType,
            SourceReference = document.SourceReference,
            UpdatedAt = document.UpdatedAt,
            ChunkIndex = document.ChunkIndex,
            Text = document.Text,
            Embedding = new Vector(document.Embedding.ToArray())
        };

    private static FeatureResearchSearchCandidate ToCandidate(
        FeatureResearchSearchEntry entry
    ) =>
        new(
            AggregateId.FromDatabaseGuid(entry.FeatureAggregateId),
            entry.FeatureName,
            entry.ResearchDiscoveryId,
            entry.Title,
            entry.SourceType,
            entry.SourceReference,
            entry.UpdatedAt,
            entry.ChunkIndex,
            entry.Text
        );
}
