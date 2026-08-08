using ActionModule.Shared;
using ActionModule.Shared.Models;
using SkillsModule.Application.DTOs;
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
