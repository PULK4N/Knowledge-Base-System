using System.Collections.Immutable;
using System.Text.Json;
using EmbeddingModule;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace PostgreSqlModule;

internal sealed class PostgreSqlKnowledgeSearchRepository(
    EventSourcingDbContext dbContext
) : IKnowledgeSearchRepository
{
    private const string Table = "KnowledgeSearchEntries";
    private const string SourceColumns = "\"OwnerType\", \"OwnerAggregateId\"";
    private const string TieBreakers = "\"Timestamp\" DESC, \"Id\"";

    public async Task Write(
        string ownerType,
        List<AggregateId> ownerAggregateIds,
        List<KnowledgeSearchDocument> documents,
        CancellationToken cancellationToken = default
    )
    {
        var ownerIds = ownerAggregateIds
            .Select(id => id.Value)
            .Distinct()
            .ToList();

        if (ownerIds.Count == 0)
            return;

        if (documents.Any(document => document.OwnerType != ownerType))
        {
            throw new ArgumentException(
                "Every document must match the replaced owner type.",
                nameof(documents)
            );
        }

        await using var transaction = dbContext.Database.CurrentTransaction is null
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        await dbContext.Set<KnowledgeSearchEntry>()
            .Where(
                entry => entry.OwnerType == ownerType
                    && ownerIds.Contains(entry.OwnerAggregateId)
            )
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Set<KnowledgeSearchEntry>().AddRangeAsync(
            documents.Select(ToEntry),
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<List<KnowledgeSearchCandidate>> SearchText(
        List<string> keywords,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateTextQuery(keywords, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<KnowledgeSearchEntry> CreateTextQuery(
        List<string> keywords,
        int candidateCount
    ) =>
        dbContext.Set<KnowledgeSearchEntry>()
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
            .ThenByDescending(entry => entry.Timestamp)
            .ThenBy(entry => entry.Id)
            .Take(candidateCount);

    public async Task<List<KnowledgeSearchCandidate>> SearchVector(
        ImmutableArray<float> embedding,
        int candidateCount,
        CancellationToken cancellationToken = default
    )
    {
        var entries = await CreateVectorQuery(embedding, candidateCount)
            .ToListAsync(cancellationToken);

        return entries.Select(ToCandidate).ToList();
    }

    internal IQueryable<KnowledgeSearchEntry> CreateVectorQuery(
        ImmutableArray<float> embedding,
        int candidateCount
    )
    {
        var vector = new Vector(embedding.ToArray());

        return dbContext.Set<KnowledgeSearchEntry>()
            .AsNoTracking()
            .OrderBy(entry => entry.Embedding.CosineDistance(vector))
            .ThenByDescending(entry => entry.Timestamp)
            .ThenBy(entry => entry.Id)
            .Take(candidateCount);
    }

    public async Task<List<KnowledgeSearchCandidate>> SearchTextBySource(
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

    internal IQueryable<KnowledgeSearchEntry> CreateTextSourceQuery(
        List<string> keywords,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<KnowledgeSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Text(Table, SourceColumns, TieBreakers),
                SourceSearchSql.TextParameters(
                    keywords,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    public async Task<List<KnowledgeSearchCandidate>> SearchVectorBySource(
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

    internal IQueryable<KnowledgeSearchEntry> CreateVectorSourceQuery(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount
    ) =>
        dbContext.Set<KnowledgeSearchEntry>()
            .FromSqlRaw(
                SourceSearchSql.Vector(Table, SourceColumns, TieBreakers),
                SourceSearchSql.VectorParameters(
                    embedding,
                    sourceCount,
                    candidateCount
                )
            )
            .AsNoTracking();

    private static KnowledgeSearchEntry ToEntry(
        KnowledgeSearchDocument document
    ) =>
        new()
        {
            OwnerType = document.OwnerType,
            OwnerAggregateId = document.OwnerAggregateId.Value,
            SourceType = document.SourceType,
            SourceKey = document.SourceKey,
            ChunkIndex = document.ChunkIndex,
            Timestamp = document.Timestamp,
            MetadataJson = document.Metadata.GetRawText(),
            SearchableMetadata = document.SearchableMetadata,
            Text = document.Text,
            Embedding = new Vector(document.Embedding.ToArray())
        };

    private static KnowledgeSearchCandidate ToCandidate(
        KnowledgeSearchEntry entry
    ) =>
        new(
            entry.Id,
            entry.OwnerType,
            AggregateId.FromDatabaseGuid(entry.OwnerAggregateId),
            entry.SourceType,
            entry.SourceKey,
            entry.ChunkIndex,
            entry.Timestamp,
            ParseMetadata(entry.MetadataJson),
            entry.Text
        );

    private static JsonElement ParseMetadata(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
