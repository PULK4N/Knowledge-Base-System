namespace SharedModule.Persistence;

public sealed class EntityRelation
{
    public long Id { get; set; }
    public required Guid EntityId { get; set; }
    public required Guid RelatedEntityId { get; set; }
    public required string RelationType { get; set; }
    public required string RelatedEntitySummary { get; set; }
}

public sealed record EntityRelationWrite(
    Guid EntityId,
    string EntitySummary,
    Guid RelatedEntityId,
    string RelatedEntitySummary,
    string RelationType,
    string ReverseRelationType
);
