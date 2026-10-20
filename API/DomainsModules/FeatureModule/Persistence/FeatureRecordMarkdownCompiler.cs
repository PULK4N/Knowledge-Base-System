using FeatureModule.Domain;

namespace FeatureModule.Persistence;

internal static class FeatureRecordMarkdownCompiler
{
    public static List<FeatureRecordSearchSource> Compile(
        FeatureStateData feature
    ) =>
        feature.Records
            .Select(
                record => new FeatureRecordSearchSource(
                    record.Id.Value,
                    record.Type,
                    record.Title,
                    record.SourceType?.ToString() ?? string.Empty,
                    record.SourceReference,
                    record.CreatedAt,
                    record.UpdatedAt,
                    $"# {record.Title}\n\n{record.Content}"
                )
            )
            .ToList();
}

internal sealed record FeatureRecordSearchSource(
    Guid RecordId,
    string RecordType,
    string Title,
    string SourceType,
    string SourceReference,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Markdown
);
