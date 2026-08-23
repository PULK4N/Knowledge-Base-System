using ActionModule.Shared.Models;
using SkillsModule.Contracts;

namespace SkillsModule.Persistence.Interfaces;

public interface ISkillListRepository
{
    Task<PagedResult<SkillListItem>> Search(
        EntityQuery<SkillSearchFilters, SkillSearchSortField> request,
        CancellationToken cancellationToken = default
    );
}

public sealed record SkillListItem(
    Guid SkillId,
    string Name,
    string Description,
    List<string> Tags,
    int ReferenceCount,
    int AttachmentCount
);
