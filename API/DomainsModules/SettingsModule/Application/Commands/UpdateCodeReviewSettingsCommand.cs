using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using SettingsModule.Application.DTOs;
using SettingsModule.Domain.Events;
using SharedModule.Constants;

namespace SettingsModule.Application.Commands;

public sealed class UpdateCodeReviewSettingsCommand(StateMachineHandler handler)
    : Command<CodeReviewSettingsDto>
{
    public int BatchSize { get; set; }
    public string TaskMessage { get; set; } = string.Empty;

    protected override async Task<CodeReviewSettingsDto> ExecuteInternal(Executor executor)
    {
        await handler.ExecuteEvents(EventPayload.Create(executor.Id,
            AggregateId.FromDatabaseGuid(StateDataAggregateIds.Settings), "settings-state-machine",
            new CodeReviewSettingsUpdatedV1(BatchSize, TaskMessage)));
        return new(BatchSize, TaskMessage);
    }
}
