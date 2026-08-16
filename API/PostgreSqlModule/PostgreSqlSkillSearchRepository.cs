using System.Collections.Immutable;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using SkillsModule.Persistence.Interfaces;

namespace PostgreSqlModule;

internal sealed class PostgreSqlSkillSearchRepository(
    EventSourcingDbContext dbContext
) : ISkillSearchRepository
{
    public async Task Write(
        IReadOnlyCollection<AggregateId> skillAggregateIds,
        IReadOnlyCollection<SkillSearchDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var aggregateIds = skillAggregateIds
            .Select(aggregateId => aggregateId.Value)
            .Distinct()
            .ToList();

        if (aggregateIds.Count == 0)
            return;

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Set<SkillSearchEntry>()
            .Where(entry => aggregateIds.Contains(entry.SkillAggregateId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<SkillSearchEntry>().AddRangeAsync(
            documents.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillSearchCandidate>> SearchText(
        string query,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(query, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<SkillSearchEntry> CreateTextQuery(
        string query,
        int candidateCount
    ) =>
        dbContext.Set<SkillSearchEntry>()
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
            .ThenBy(entry => entry.SkillName)
            .ThenBy(entry => entry.SourcePath)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);

    public async Task<IReadOnlyList<SkillSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorQuery(embedding, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<SkillSearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount
    )
    {
        var vector = new Vector(embedding.ToArray());

        return dbContext.Set<SkillSearchEntry>()
            .AsNoTracking()
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenBy(entry => entry.SkillName)
            .ThenBy(entry => entry.SourcePath)
            .ThenBy(entry => entry.ChunkIndex)
            .Take(candidateCount);
    }

    private static SkillSearchEntry ToEntry(
        SkillSearchDocument document
    ) =>
        new()
        {
            SkillAggregateId = document.SkillAggregateId.Value,
            SkillName = document.SkillName,
            SourcePath = document.SourcePath,
            ChunkIndex = document.ChunkIndex,
            Text = document.Text,
            Embedding = new Vector(document.Embedding.ToArray())
        };

    private static SkillSearchCandidate ToCandidate(
        SkillSearchEntry entry
    ) =>
        new(
            AggregateId.FromDatabaseGuid(entry.SkillAggregateId),
            entry.SkillName,
            entry.SourcePath,
            entry.ChunkIndex,
            entry.Text
        );
}
