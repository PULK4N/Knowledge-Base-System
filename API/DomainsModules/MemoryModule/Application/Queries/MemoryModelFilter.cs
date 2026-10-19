namespace MemoryModule.Application.Queries;

public static class MemoryModelFilter
{
    public const int MaximumLength = 200;

    public static bool IsValid(string? model) =>
        model is null || model.Length <= MaximumLength;

    public static string? Normalize(string? model) =>
        string.IsNullOrWhiteSpace(model) ? null : model.Trim();
}
