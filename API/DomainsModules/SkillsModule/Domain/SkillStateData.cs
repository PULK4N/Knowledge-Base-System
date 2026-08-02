using EventSourcing.Shared.Models;
using SkillsModule.Domain.Models;

namespace SkillsModule.Domain;

public sealed class SkillStateData(AggregateId id) : ISharedStateData
{
    public AggregateId Id { get; init; } = id;
    public bool IsDeleted { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, SkillReference> References { get; set; } =
        new(StringComparer.Ordinal);
    public Dictionary<FileId, Attachment> Attachments { get; set; } = [];
}
