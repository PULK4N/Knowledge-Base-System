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
    public required IReadOnlyDictionary<Guid, AttachmentDto> Attachments
    {
        get;
        init;
    }

    public static SkillDto FromStateData(SkillStateData stateData) =>
        new()
        {
            Id = stateData.Id.Value,
            IsDeleted = stateData.IsDeleted,
            Name = stateData.Name,
            Description = stateData.Description,
            Content = stateData.Content,
            Tags = stateData.Tags.ToArray(),
            References = stateData.References.ToDictionary(
                reference => reference.Key,
                reference => SkillReferenceDto.FromModel(
                    reference.Value
                ),
                StringComparer.Ordinal
            ),
            Attachments = stateData.Attachments.ToDictionary(
                attachment => attachment.Key.Value,
                attachment => AttachmentDto.FromModel(
                    attachment.Value
                )
            )
        };
}

public sealed record SkillReferenceDto(string Content)
{
    public static SkillReferenceDto FromModel(
        SkillReference reference
    ) =>
        new(reference.Content);
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
