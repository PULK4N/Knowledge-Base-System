using ActionModule.Shared;
using ActionModule.Shared.Models;
using ProjectModule.Application.DTOs;
using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Application.Queries;

public sealed class ListPolicyProjectsQuery(
    IPolicyProjectSummaryRepository repository
) : Query<List<PolicyProjectSummaryDto>>
{
    protected override async Task<List<PolicyProjectSummaryDto>> ExecuteInternal(
        Executor executor
    ) =>
        (await repository.List())
            .Select(PolicyProjectSummaryDto.FromReadModel)
            .ToList();
}
