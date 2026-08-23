using ActionModule.Shared;
using ActionModule.Shared.Models;
using SkillsModule.Application.DTOs;
using SkillsModule.Contracts;
using SkillsModule.Persistence.Interfaces;

namespace SkillsModule.Application.Queries;

public sealed class ListSkillsQuery(
    ISkillSummaryRepository skillSummaryRepository
) : Query<List<SkillSummaryDto>>
{
    protected override async Task<List<SkillSummaryDto>> ExecuteInternal(
        Executor executor
    ) =>
        (await skillSummaryRepository.List())
            .Select(SkillSummaryDto.FromReadModel)
            .ToList();
}

public sealed class GetSkillByNameQuery(
    ISkillSummaryRepository skillSummaryRepository
) : Query<SkillSummaryDto?>
{
    public required string Name { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override async Task<SkillSummaryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var skill = await skillSummaryRepository.GetByName(Name);

        return skill is null
            ? null
            : SkillSummaryDto.FromReadModel(skill);
    }
}

public sealed class SearchSkillsQuery(
    ISkillListRepository skillListRepository
) : PagedQuery<SkillListItemDto>
{
    public string? Tag { get; set; }
    public bool? HasReferences { get; set; }
    public bool? HasAttachments { get; set; }
    public SkillSearchSortField SortBy { get; set; } =
        SkillSearchSortField.Name;
    public SortDirection SortDirection { get; set; } =
        SortDirection.Ascending;

    public override async Task<bool> CanExecute(Executor executor) =>
        await base.CanExecute(executor)
        && (Tag?.Length ?? 0) <= EntityQueryLimits.MaximumSearchLength
        && Enum.IsDefined(SortBy)
        && Enum.IsDefined(SortDirection);

    protected override async Task<PagedResult<SkillListItemDto>> ExecuteInternal(
        Executor executor
    )
    {
        var result = await skillListRepository.Search(
            CreateEntityQuery(
                new SkillSearchFilters(
                    Tag,
                    HasReferences,
                    HasAttachments
                ),
                SortBy,
                SortDirection
            )
        );

        return result.Map(SkillListItemDto.FromReadModel);
    }
}
