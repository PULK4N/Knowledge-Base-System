using System.Collections.Immutable;
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
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(query, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<FeatureResearchSearchEntry> CreateTextQuery(
        string query,
        int candidateCount
    ) =>
        dbContext.Set<FeatureResearchSearchEntry>()
            .AsNoTracking()
            .Where(
                entry => entry.SearchVector.Matches(
                    EF.Functions.WebSearchToTsQuery("simple", query)
                )
            )
            .OrderByDescending(
                entry => entry.SearchVector.RankCoverDensity(
                    EF.Functions.WebSearchToTsQuery("simple", query)
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
