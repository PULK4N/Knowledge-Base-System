namespace MemoryModule.Persistence;

public static class MemoryModelNames
{
    /// <summary>
    /// Returns the model name to store in read models, or null when the prompt
    /// has no model. Codex records an empty name when its hook has no model.
    /// </summary>
    public static string? Normalize(string? model) =>
        string.IsNullOrWhiteSpace(model) ? null : model.Trim();
}
