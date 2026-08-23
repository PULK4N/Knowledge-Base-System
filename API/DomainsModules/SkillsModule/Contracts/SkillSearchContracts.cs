namespace SkillsModule.Contracts;

public sealed record SkillSearchFilters(
    string? Tag,
    bool? HasReferences,
    bool? HasAttachments
);

public enum SkillSearchSortField
{
    Name,
    ReferenceCount,
    AttachmentCount
}
