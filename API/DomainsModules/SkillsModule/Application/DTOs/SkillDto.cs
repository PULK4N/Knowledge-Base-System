using SkillsModule.Domain;
using SkillsModule.Domain.Models;

namespace SkillsModule.Application.DTOs;

public sealed record SkillDto
{
    public required Guid Id { get; init; }
    public required bool IsDeleted { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Content { get; init; }
    public required IReadOnlyCollection<string> Tags { get; init; }
    public required IReadOnlyDictionary<string, SkillReferenceDto> References
    {
        get;
        init;
    }
    public required IReadOnlyCollection<string> OtherReferences
    {
        get;
        init;
    }
    public required IReadOnlyDictionary<Guid, AttachmentDto> Attachments
    {
        get;
        init;
    }
    public required IReadOnlyCollection<SkillMemoryHistoryDto> MemoryHistory
    {
        get;
        init;
    }

    public static SkillDto FromStateData(
        SkillStateData stateData,
        bool includeAllReferences = true
    ) =>
        new()
        {
            Id = stateData.Id.Value,
            IsDeleted = stateData.IsDeleted,
            Name = stateData.Name,
            Description = stateData.Description,
            Content = stateData.Content,
            Tags = stateData.Tags.ToList(),
            References = stateData.References
                .Where(
                    reference =>
                        includeAllReferences
                        || reference.Value.LoadAutomatically
                )
                .ToDictionary(
                    reference => reference.Key,
                    reference => SkillReferenceDto.FromModel(
                        reference.Value
                    ),
                    StringComparer.Ordinal
                ),
            OtherReferences = includeAllReferences
                ? []
                : stateData.References
                    .Where(
                        reference =>
                            !reference.Value.LoadAutomatically
                    )
                    .Select(reference => reference.Key)
                    .Order(StringComparer.Ordinal)
                    .ToList(),
            Attachments = stateData.Attachments.ToDictionary(
                attachment => attachment.Key.Value,
                attachment => AttachmentDto.FromModel(
                    attachment.Value
                )
            ),
            MemoryHistory = stateData.MemoryHistory
                .Select(SkillMemoryHistoryDto.FromModel)
                .ToList()
        };
}

public sealed record SkillReferenceDto(
    string Content,
    bool LoadAutomatically
)
{
    public static SkillReferenceDto FromModel(
        SkillReference2 reference
    ) =>
        new(reference.Content, reference.LoadAutomatically);
}

public sealed record AttachmentDto(
    Guid Id,
    string Name,
    long Size,
    string FileType,
    string Extension
)
{
    public static AttachmentDto FromModel(Attachment attachment) =>
        new(
            attachment.Id.Value,
            attachment.Name,
            attachment.Size,
            attachment.FileType,
            attachment.Extension
        );
}

public sealed record SkillMemoryHistoryDto(
    string EventName,
    DateTime Timestamp,
    Guid MemoryId,
    bool IsUserOriginated
)
{
    public static SkillMemoryHistoryDto FromModel(MemoryHistoryRecord record) =>
        new(
            record.EventName,
            record.Timestamp,
            record.AggregateId.Value,
            record.AggregateId.Value == SharedModule.Constants.MemoryAggregateIds.User
        );
}
