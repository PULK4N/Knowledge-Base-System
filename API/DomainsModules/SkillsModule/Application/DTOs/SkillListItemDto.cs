using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.DTOs;

public sealed record SkillListItemDto(
    Guid SkillId,
    string Name,
    string Description,
    List<string> Tags,
    int ReferenceCount,
    int AttachmentCount
)
{
    public static SkillListItemDto FromReadModel(
        SkillListItem readModel
    ) =>
        new(
            readModel.SkillId,
            readModel.Name,
            readModel.Description,
            readModel.Tags,
            readModel.ReferenceCount,
            readModel.AttachmentCount
        );
}
