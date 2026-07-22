using System.ComponentModel;
using MemoryMcp.Services;
using ModelContextProtocol.Server;

namespace MemoryMcp.Tools;

[McpServerToolType]
public sealed class PolicyTools(MemoryService memory)
{
    [McpServerTool(
        Name = "policy_save",
        Title = "Save policy",
        Destructive = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Saves a durable instruction describing how agents should or should not work.")]
    public Task<PolicyDto> Save(
        [Description("Short unique policy name.")] string name,
        [Description("The complete instruction an agent should follow.")] string instruction,
        [Description("Applicability such as global, repository, dotnet, or reviews.")]
            string scope = "global",
        [Description("Ordering priority from -1000 to 1000; higher policies are returned first.")]
            int priority = 0,
        [Description("Whether agents should currently apply this policy.")] bool enabled = true,
        [Description("Optional comma-separated search tags.")] string tags = "",
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) =>
        memory.SavePolicy(
            name,
            instruction,
            scope,
            priority,
            enabled,
            ToolInput.Tags(tags),
            ToolInput.Executor(executorId)
        );

    [McpServerTool(
        Name = "policy_update",
        Title = "Update policy",
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Updates selected fields of an active policy. Omitted fields are preserved.")]
    public Task<PolicyDto> Update(
        [Description("Policy UUID returned by policy_save or policy_search.")] string id,
        [Description("Replacement name.")] string? name = null,
        [Description("Replacement instruction.")] string? instruction = null,
        [Description("Replacement applicability scope.")] string? scope = null,
        [Description("Replacement priority from -1000 to 1000.")] int? priority = null,
        [Description("Replacement enabled state.")] bool? enabled = null,
        [Description("Replacement comma-separated tags; pass an empty string to clear tags.")]
            string? tags = null,
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) =>
        memory.UpdatePolicy(
            ToolInput.Id(id, nameof(id)),
            name,
            instruction,
            scope,
            priority,
            enabled,
            tags is null ? null : ToolInput.Tags(tags),
            ToolInput.Executor(executorId)
        );

    [McpServerTool(
        Name = "policy_delete",
        Title = "Delete policy",
        Destructive = true,
        Idempotent = false,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Soft-deletes a policy while preserving its event history for audit.")]
    public Task<DeleteResult> Delete(
        [Description("Policy UUID.")] string id,
        [Description("Optional reason retained in the deletion event.")] string reason = "",
        [Description("Optional UUID identifying the agent or user making the change.")]
            string? executorId = null
    ) => memory.DeletePolicy(ToolInput.Id(id, nameof(id)), reason, ToolInput.Executor(executorId));

    [McpServerTool(
        Name = "policy_get",
        Title = "Get policy",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description("Gets one policy by UUID by replaying its event history.")]
    public Task<PolicyDto?> Get(
        [Description("Policy UUID.")] string id,
        [Description("Whether a deleted policy may be returned.")] bool includeDeleted = false
    ) => memory.GetPolicy(ToolInput.Id(id, nameof(id)), includeDeleted);

    [McpServerTool(
        Name = "policy_search",
        Title = "Search policies",
        ReadOnly = true,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true
    )]
    [Description(
        "Searches applicable policies. Enabled, high-priority policies are returned first by default."
    )]
    public Task<IReadOnlyList<PolicyDto>> Search(
        [Description("Space-separated terms matched against name, instruction, scope, and tags.")]
            string query = "",
        [Description("Optional exact applicability-scope filter.")] string? scope = null,
        [Description("Optional exact tag filter.")] string? tag = null,
        [Description("Whether disabled policies are included.")] bool includeDisabled = false,
        [Description("Whether deleted policies are included.")] bool includeDeleted = false,
        [Description("Maximum results from 1 to 100.")] int limit = 20
    ) => memory.SearchPolicies(query, scope, tag, includeDisabled, includeDeleted, limit);
}
