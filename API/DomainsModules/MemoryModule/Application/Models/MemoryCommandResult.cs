namespace MemoryModule.Application.Models;

public sealed record MemoryCommandResult(
    string Status
)
{
    public static MemoryCommandResult Ok { get; } = new("OK");
}
