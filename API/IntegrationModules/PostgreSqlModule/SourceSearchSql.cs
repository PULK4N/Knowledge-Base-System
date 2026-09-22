using System.Collections.Immutable;
using System.Globalization;
using EmbeddingModule;
using Npgsql;
using NpgsqlTypes;

namespace PostgreSqlModule;

/// <summary>
/// Builds the source-grouped companion queries for the hybrid search
/// projections. PostgreSQL DISTINCT ON keeps the best ranked row per owning
/// source, so one skill, feature, or memory cannot fill a whole result set
/// with its own chunks. The inner query bounds the work to the ranked
/// candidate window, which keeps the GIN and HNSW indexes usable.
/// </summary>
internal static class SourceSearchSql
{
    private const string KeywordsParameterName = "keywords";
    private const string EmbeddingParameterName = "embedding";
    private const string SourceCountParameterName = "sourceCount";
    private const string CandidateCountParameterName = "candidateCount";

    // plainto_tsquery ANDs every word and ignores operator syntax, so a row
    // matches only when it contains all of the caller's keywords.
    private const string TextQueryExpression =
        $"plainto_tsquery('simple', @{KeywordsParameterName})";
    private const string TextRelevance =
        $"ts_rank_cd(\"SearchVector\", {TextQueryExpression}) DESC";
    private const string VectorRelevance =
        $"\"Embedding\" <=> @{EmbeddingParameterName}::vector";

    internal static string Text(
        string table,
        string sourceColumns,
        string tieBreakers
    ) =>
        $"""
        SELECT * FROM (
            SELECT DISTINCT ON ({sourceColumns}) * FROM (
                SELECT * FROM "{table}"
                WHERE "SearchVector" @@ {TextQueryExpression}
                ORDER BY {TextRelevance}, {tieBreakers}
                LIMIT @{CandidateCountParameterName}
            ) AS candidates
            ORDER BY {sourceColumns}, {TextRelevance}, {tieBreakers}
        ) AS sources
        ORDER BY {TextRelevance}, {tieBreakers}
        LIMIT @{SourceCountParameterName}
        """;

    internal static string Vector(
        string table,
        string sourceColumns,
        string tieBreakers
    ) =>
        $"""
        SELECT * FROM (
            SELECT DISTINCT ON ({sourceColumns}) * FROM (
                SELECT * FROM "{table}"
                ORDER BY {VectorRelevance}, {tieBreakers}
                LIMIT @{CandidateCountParameterName}
            ) AS candidates
            ORDER BY {sourceColumns}, {VectorRelevance}, {tieBreakers}
        ) AS sources
        ORDER BY {VectorRelevance}, {tieBreakers}
        LIMIT @{SourceCountParameterName}
        """;

    // FromSqlRaw takes params object[], so the parameter set stays array shaped.
    internal static object[] TextParameters(
        List<string> keywords,
        int sourceCount,
        int candidateCount
    ) =>
        [
            new NpgsqlParameter(KeywordsParameterName, NpgsqlDbType.Text)
            {
                Value = SearchKeywordLimits.ToTextQuery(keywords)
            },
            SourceCount(sourceCount),
            CandidateCount(candidateCount)
        ];

    internal static object[] VectorParameters(
        ImmutableArray<float> embedding,
        int sourceCount,
        int candidateCount
    ) =>
        [
            new NpgsqlParameter(EmbeddingParameterName, NpgsqlDbType.Text)
            {
                Value = Literal(embedding)
            },
            SourceCount(sourceCount),
            CandidateCount(candidateCount)
        ];

    private static NpgsqlParameter SourceCount(int sourceCount) =>
        new(SourceCountParameterName, NpgsqlDbType.Integer)
        {
            Value = sourceCount
        };

    private static NpgsqlParameter CandidateCount(int candidateCount) =>
        new(CandidateCountParameterName, NpgsqlDbType.Integer)
        {
            Value = candidateCount
        };

    /// <summary>
    /// Formats the embedding as a pgvector literal that the query casts back to
    /// vector, so the parameter never depends on culture-specific formatting.
    /// </summary>
    private static string Literal(ImmutableArray<float> embedding) =>
        "["
        + string.Join(
            ",",
            embedding.Select(
                value => value.ToString("R", CultureInfo.InvariantCulture)
            )
        )
        + "]";
}
