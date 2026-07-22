using System.ComponentModel;
using MemoryMcp.Services;
using ModelContextProtocol.Server;

namespace MemoryMcp.Tools;

[McpServerToolType]
public sealed class SkillTools(MemoryService memory)
{
    [McpServerTool(
        Name = "skill_save",
        Title = "Save skill",
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Saves a reusable agent skill. Skill names must be unique while active.")]
    public Task<SkillDto> Save(
        [Description("Short unique skill name.")] string name,
        [Description("What the skill is for and when an agent should use it.")] string description,
        [Description("Complete skill instructions or knowledge to remember.")] string content,
        [Description("Optional comma-separated search tags.")] string tags = "",
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) =>
        memory.SaveSkill(
            name,
            description,
            content,
            ToolInput.Tags(tags),
            ToolInput.Executor(executorId)
        );

    [McpServerTool(
        Name = "skill_update",
        Title = "Update skill",
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description(
        "Updates selected fields of an existing active skill. Omitted fields are preserved."
    )]
    public Task<SkillDto> Update(
        [Description("Skill UUID returned by skill_save or skill_search.")] string id,
        [Description("Replacement name, or omit to preserve it.")] string? name = null,
        [Description("Replacement description, or omit to preserve it.")]
            string? description = null,
        [Description("Replacement content, or omit to preserve it.")] string? content = null,
        [Description("Replacement comma-separated tags; pass an empty string to clear tags.")]
            string? tags = null,
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) =>
        memory.UpdateSkill(
            ToolInput.Id(id, nameof(id)),
            name,
            description,
            content,
            tags is null ? null : ToolInput.Tags(tags),
            ToolInput.Executor(executorId)
        );

    [McpServerTool(
        Name = "skill_delete",
        Title = "Delete skill",
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Soft-deletes a skill while preserving its complete event history for audit.")]
    public Task<DeleteResult> Delete(
        [Description("Skill UUID.")] string id,
        [Description("Optional reason retained in the deletion event.")] string reason = "",
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) => memory.DeleteSkill(ToolInput.Id(id, nameof(id)), reason, ToolInput.Executor(executorId));

    [McpServerTool(
        Name = "skill_get",
        Title = "Get skill",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Gets one skill by UUID by replaying its event history.")]
    public Task<SkillDto?> Get(
        [Description("Skill UUID.")] string id,
        [Description("Whether a deleted skill may be returned.")] bool includeDeleted = false
    ) => memory.GetSkill(ToolInput.Id(id, nameof(id)), includeDeleted);

    [McpServerTool(
        Name = "skill_search",
        Title = "Search skills",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Searches current skills across names, descriptions, content, and tags.")]
    public Task<IReadOnlyList<SkillDto>> Search(
        [Description("Space-separated search terms; empty returns recent skills.")]
            string query = "",
        [Description("Optional exact tag filter.")] string? tag = null,
        [Description("Whether deleted skills are included.")] bool includeDeleted = false,
        [Description("Maximum results from 1 to 100.")] int limit = 20
    ) => memory.SearchSkills(query, tag, includeDeleted, limit);
}
