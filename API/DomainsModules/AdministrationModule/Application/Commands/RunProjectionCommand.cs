using ActionModule.Shared;
using ActionModule.Shared.Models;
using AdministrationModule.Application.DTOs;
using AdministrationModule.Application.Persistence;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace AdministrationModule.Application.Commands;

public sealed class RunProjectionCommand : Command<ProjectionRunResult?>
{
    private readonly IProjectionReplayRepository _replayRepository;
    private readonly IEventStore _eventStore;
    private readonly StateCalculator _stateCalculator;
    private readonly IReadOnlyDictionary<string, IProjector> _projectors;

    public RunProjectionCommand(
        IProjectionReplayRepository replayRepository,
        IEventStore eventStore,
        StateCalculator stateCalculator,
        IEnumerable<IProjector> projectors
    )
    {
        _replayRepository = replayRepository;
        _eventStore = eventStore;
        _stateCalculator = stateCalculator;
        _projectors = projectors.ToDictionary(
            projector => projector.GetType().Name,
            StringComparer.Ordinal
        );
    }

    public required string ProjectionName { get; set; }
    public Guid? AggregateId { get; set; }
    public string? StateMachineId { get; set; }

    public override Task<bool> CanExecute(Executor executor)
    {
        var hasAggregateId = AggregateId is { } aggregateId
            && aggregateId != Guid.Empty;
        var hasStateMachineId = !string.IsNullOrWhiteSpace(
            StateMachineId
        );

        return Task.FromResult(
            !string.IsNullOrWhiteSpace(ProjectionName)
            && _projectors.ContainsKey(ProjectionName)
            && hasAggregateId != hasStateMachineId
        );
    }

    protected override async Task<ProjectionRunResult?> ExecuteInternal(
        Executor executor
    )
    {
        var aggregateIds = await GetAggregateIds();
        if (aggregateIds.Count == 0)
            return ProjectionRunResult.Completed(0);

        var histories = await _eventStore.GetEvents(aggregateIds);
        if (
            AggregateId.HasValue
            && (
                !histories.TryGetValue(
                    aggregateIds[0],
                    out var requestedHistory
                )
                || requestedHistory.Count == 0
            )
        )
            return null;

        var stateInfos = new List<StateInfo>();
        foreach (var aggregateId in aggregateIds)
        {
            if (
                !histories.TryGetValue(
                    aggregateId,
                    out var aggregateHistory
                )
                || aggregateHistory.Count == 0
            )
                continue;

            stateInfos.Add(
                await _stateCalculator.Calculate(
                    aggregateHistory,
                    []
                )
            );
        }

        if (stateInfos.Count > 0)
            await _projectors[ProjectionName].Update(stateInfos);

        return ProjectionRunResult.Completed(stateInfos.Count);
    }

    private async Task<List<AggregateId>> GetAggregateIds()
    {
        if (AggregateId is { } aggregateId)
        {
            return
            [
                EventSourcing.Shared.Models.AggregateId
                    .FromDatabaseGuid(aggregateId)
            ];
        }

        return (await _replayRepository.GetLastEvents(StateMachineId!))
            .Select(payload => payload.EventExecutionInfo.AggregateId)
            .Distinct()
            .ToList();
    }
}
