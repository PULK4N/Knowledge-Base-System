using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Domain;

namespace ProjectModule.Application.Queries;

/// <summary>
/// Returns one active project policy with the changes recorded for it. History
/// is kept per project, so the project's records are filtered by policy.
/// </summary>
public sealed class GetProjectPolicyHistoryQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore
) : ProjectQuery<PolicyHistoryDto?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }

    public required Guid PolicyId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty && PolicyId != Guid.Empty);

    protected override async Task<PolicyHistoryDto?> ExecuteInternal(
        Executor executor
    )
    {
        var projectId = AggregateId.FromDatabaseGuid(ProjectId);
        var project = await Replay<ProjectStateData>(
            await GetEvents([projectId]),
            projectId
        );

        return project is null
            || project.IsDeleted
            || !project.Policies.TryGetValue(
                PolicyModule.Shared.PolicyId.FromDatabaseGuid(PolicyId),
                out var policy
            )
            ? null
            : PolicyHistoryDto.FromModel(policy, project.MemoryHistory);
    }
}
