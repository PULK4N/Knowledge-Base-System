using ActionModule.Shared;
using ActionModule.Shared.Models;
using ProjectModule.Application.DTOs;
using ProjectModule.Persistence.Interfaces;

namespace ProjectModule.Application.Queries;

public sealed class GetPolicyProjectByNameQuery(
    IPolicyProjectSummaryRepository repository
) : Query<PolicyProjectSummaryDto?>
{
    public required string Name { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(!string.IsNullOrWhiteSpace(Name));

    protected override async Task<PolicyProjectSummaryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var project = await repository.GetByName(Name);

        return project is null
            ? null
            : PolicyProjectSummaryDto.FromReadModel(project);
    }
}
