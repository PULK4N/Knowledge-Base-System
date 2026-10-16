namespace MemoryModule.Application.Queries;

/// <summary>
/// Validates and normalizes the activity range used to find memories by time.
/// Read-model timestamps are UTC, so a time without a zone is read as UTC.
/// </summary>
public static class MemoryActivityRange
{
    public static bool IsValid(DateTime? from, DateTime? to) =>
        from is null
        || to is null
        || ToUtc(from.Value) <= ToUtc(to.Value);

    public static DateTime? ToUtc(DateTime? value) =>
        value is null ? null : ToUtc(value.Value);

    private static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
