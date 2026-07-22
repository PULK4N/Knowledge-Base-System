using MemoryMcp.Domain.Policies;
using MemoryMcp.Domain.Skills;

namespace MemoryMcp.Services;

public sealed record SkillDto(
    Guid Id,
    string Name,
    string Description,
    string Content,
    string[] Tags,
    bool IsDeleted,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version
)
{
    public static SkillDto From(SkillState state) =>
        new(
            state.Id,
            state.Name,
            state.Description,
            state.Content,
            [ .. state.Tags ],
            state.IsDeleted,
            state.CreatedAtUtc,
            state.UpdatedAtUtc,
            state.Version
        );
}

public sealed record PolicyDto(
    Guid Id,
    string Name,
    string Instruction,
    string Scope,
    int Priority,
    bool Enabled,
    string[] Tags,
    bool IsDeleted,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    uint Version
)
{
    public static PolicyDto From(PolicyState state) =>
        new(
            state.Id,
            state.Name,
            state.Instruction,
            state.Scope,
            state.Priority,
            state.Enabled,
            [ .. state.Tags ],
            state.IsDeleted,
            state.CreatedAtUtc,
            state.UpdatedAtUtc,
            state.Version
        );
}

public sealed record DeleteResult(Guid Id, bool Deleted, uint Version);
