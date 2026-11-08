using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using ProjectModule.Application.DTOs;
using ProjectModule.Domain;
using SkillsModule.Persistence.Interfaces;

namespace ProjectModule.Application.Queries;

public sealed class GetPolicyProjectQuery(
    StateCalculator stateCalculator,
    IEventStore eventStore,
    ISkillSummaryRepository skillSummaryRepository
) : ProjectQuery<PolicyProjectDetailsDto?>(stateCalculator, eventStore)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected override async Task<PolicyProjectDetailsDto?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateId = AggregateId.FromDatabaseGuid(ProjectId);
        var state = await Replay<ProjectStateData>(
            await GetEvents([aggregateId]),
            aggregateId
        );

        if (state is null || state.IsDeleted)
            return null;

        var skills = await skillSummaryRepository.GetUseScenarios(
            state.RelatedSkillIds.Select(skillId => skillId.Value).ToList()
        );

        return PolicyProjectDetailsDto.FromStateData(state, skills);
    }
}
