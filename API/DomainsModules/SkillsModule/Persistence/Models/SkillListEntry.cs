namespace SkillsModule.Persistence.Models;

public sealed class SkillListEntry
{
    public int Id { get; set; }
    public required Guid SkillAggregateId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Description { get; set; }
    public required string SearchText { get; set; }
    public bool IsDeleted { get; set; }
    public int ReferenceCount { get; set; }
    public int AttachmentCount { get; set; }
    public long ProjectedOrderNumber { get; set; }
    public List<SkillListTagEntry> Tags { get; set; } = [];
}

public sealed class SkillListTagEntry
{
    public int Id { get; set; }
    public int SkillListEntryId { get; set; }
    public required string Tag { get; set; }
    public required string NormalizedTag { get; set; }
}
