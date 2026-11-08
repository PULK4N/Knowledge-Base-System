using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Persistence.Interfaces;
using EventSourcing.Shared.Models;
using SettingsModule.Application.DTOs;
using SettingsModule.Domain;
using SharedModule.Constants;

namespace SettingsModule.Application.Queries;

/// <summary>
/// Reads the global settings by replaying the singleton settings aggregate; settings have no projection.
/// </summary>
public sealed class GetCodeReviewSettingsQuery(IEventStore eventStore, StateCalculator calculator)
    : Query<CodeReviewSettingsDto?>
{
    protected override async Task<CodeReviewSettingsDto?> ExecuteInternal(Executor executor)
    {
        var id = AggregateId.FromDatabaseGuid(StateDataAggregateIds.Settings);
        var events = (await eventStore.GetEvents([id])).GetValueOrDefault(id) ?? [];
        if (events.Count == 0)
            return null;
        var state = (SettingsStateData)calculator.Calculate(
            events.OrderBy(value => value.EventExecutionInfo.OrderNumber).ToList(), []).StateData;
        return state.IsInitialized ? new(state.CodeReviewBatchSize, state.CodeReviewTaskMessage) : null;
    }
}
