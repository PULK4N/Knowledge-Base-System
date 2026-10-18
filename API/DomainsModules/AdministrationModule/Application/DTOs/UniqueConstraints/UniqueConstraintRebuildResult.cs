namespace AdministrationModule.Application.DTOs.UniqueConstraints;

public sealed record UniqueConstraintRebuildResult(
    string Status,
    int ProcessedAggregateCount,
    int ConstraintCount,
    List<UniqueConstraintConflictDto> Conflicts
)
{
    public const string CompletedStatus = "Completed";
    public const string ConflictsStatus = "Conflicts";

    public static UniqueConstraintRebuildResult Completed(
        int processedAggregateCount,
        int constraintCount
    ) =>
        new(CompletedStatus, processedAggregateCount, constraintCount, []);

    public static UniqueConstraintRebuildResult WithConflicts(
        int processedAggregateCount,
        int constraintCount,
        List<UniqueConstraintConflictDto> conflicts
    ) =>
        new(ConflictsStatus, processedAggregateCount, constraintCount, conflicts);
}
