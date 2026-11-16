namespace FeatureModule.Application.Commands;

/// <summary>Null values keep the current value; an empty list removes every definition-of-done link.</summary>
public sealed record ScriptChangeInput(
    Guid Id,
    string? Path = null,
    List<Guid>? DefinitionOfDoneIds = null
);
