using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using FeatureModule.Application.Models;

namespace FeatureModule.Application.Commands;

public abstract class FeatureCommand(
    StateMachineHandler stateMachineHandler
) : Command<object>
{
    protected const string StateMachineId = "features-state-machine";

    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

    protected async Task<object> ExecuteEvent(
        Executor executor,
        AggregateId featureId,
        IEvent eventData
    )
    {
        var payload = EventPayload.Create(
            executor.Id,
            featureId,
            StateMachineId,
            eventData
        );

        await stateMachineHandler.ExecuteEvents(payload);
        return FeatureCommandResult.Ok;
    }
}

public abstract class ExistingFeatureCommand(
    StateMachineHandler stateMachineHandler
) : FeatureCommand(stateMachineHandler)
{
    public required Guid FeatureId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty);

    protected Task<object> ExecuteEvent(
        Executor executor,
        IEvent eventData
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(FeatureId),
            eventData
        );
}
