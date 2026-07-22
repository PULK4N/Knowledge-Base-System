using MemoryMcp.Domain;

namespace MemoryMcp.Tools;

internal static class ToolInput
{
    public static Guid Id(string value, string parameterName)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new ArgumentException($"{parameterName} must be a non-empty UUID.", parameterName);
        return id;
    }

    public static Guid Executor(string? value) => string.IsNullOrWhiteSpace(value)
        ? MemoryConstants.SystemExecutor
        : Id(value, "executorId");

    public static string[] Tags(string value) => value.Split(
        ',',
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
    );
}
