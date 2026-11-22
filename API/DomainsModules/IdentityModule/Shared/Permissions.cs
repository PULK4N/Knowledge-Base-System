namespace IdentityModule.Shared;

/// <summary>
/// Permissions an agent account can be given. A <c>write</c> permission also grants the
/// <c>read</c> permission of the same area. Administrators have every permission.
/// </summary>
public static class Permissions
{
    public const string MemoryRead = "memory.read";
    public const string MemoryWrite = "memory.write";
    public const string SkillsRead = "skills.read";
    public const string SkillsWrite = "skills.write";
    public const string FeaturesRead = "features.read";
    public const string FeaturesWrite = "features.write";
    public const string PoliciesRead = "policies.read";
    public const string PoliciesSuggest = "policies.suggest";
    public const string PoliciesWrite = "policies.write";
    public const string ProjectsRead = "projects.read";
    public const string ProjectsWrite = "projects.write";
    public const string AttachmentsRead = "attachments.read";
    public const string AttachmentsWrite = "attachments.write";
    public const string KnowledgeRead = "knowledge.read";

    public static readonly IReadOnlyList<string> All =
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
