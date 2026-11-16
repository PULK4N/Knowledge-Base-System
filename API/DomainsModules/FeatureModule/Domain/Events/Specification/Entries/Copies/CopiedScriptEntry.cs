using System.Collections.Immutable;
using FeatureModule.Domain.Models;

namespace FeatureModule.Domain.Events;

/// <summary>A feature copy of a plan script; DefinitionOfDoneIds point to feature definitions of done.</summary>
public sealed record CopiedScriptEntry(
    ScriptId Id,
    ScriptId OriginId,
    string Path,
    ImmutableArray<DefinitionOfDoneId> DefinitionOfDoneIds,
    ScriptSnapshot? Snapshot
);
