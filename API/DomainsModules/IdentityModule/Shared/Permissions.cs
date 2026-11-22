using ActionModule.Shared.Models;

namespace IdentityModule.Shared;

/// <summary>Permissions an agent account can be given. Administrators have every permission.</summary>
public static class Permissions
{
    public static readonly Permission MemoryRead = new("memory.read");
    public static readonly Permission MemoryWrite = new("memory.write");
    public static readonly Permission SkillsRead = new("skills.read");
    public static readonly Permission SkillsWrite = new("skills.write");
    public static readonly Permission FeaturesRead = new("features.read");
    public static readonly Permission FeaturesWrite = new("features.write");
    public static readonly Permission PoliciesRead = new("policies.read");
    public static readonly Permission PoliciesSuggest = new("policies.suggest");
    public static readonly Permission PoliciesWrite = new("policies.write");
    public static readonly Permission ProjectsRead = new("projects.read");
    public static readonly Permission ProjectsWrite = new("projects.write");
    public static readonly Permission AttachmentsRead = new("attachments.read");
    public static readonly Permission AttachmentsWrite = new("attachments.write");
    public static readonly Permission KnowledgeRead = new("knowledge.read");

    public static readonly IReadOnlyList<Permission> All =
    [
        MemoryRead, MemoryWrite,
        SkillsRead, SkillsWrite,
        FeaturesRead, FeaturesWrite,
        PoliciesRead, PoliciesSuggest, PoliciesWrite,
        ProjectsRead, ProjectsWrite,
        AttachmentsRead, AttachmentsWrite,
        KnowledgeRead
    ];
}
