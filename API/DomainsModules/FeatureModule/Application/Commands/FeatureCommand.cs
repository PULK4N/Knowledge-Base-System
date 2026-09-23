using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Constants = MemoryModule.Application.Constants;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using FeatureModule.Application.Models;

namespace FeatureModule.Application.Commands;

public abstract class FeatureCommand(StateMachineHandler stateMachineHandler)
    : Command<object>
{
    protected const string StateMachineId = "features-state-machine";

    private bool _isUserOriginated;

    public void UseUserOrigin()
    {
        _isUserOriginated = true;
        SessionId = Guid.Empty;
        MemoryAggregateId = SharedModule.Constants.MemoryAggregateIds.User;
    }

    public Guid SessionId { get; set; }
    public Guid MemoryAggregateId { get; set; }

    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

    protected async Task<AggregateId> ResolveMemoryAggregateId()
    {
        if (_isUserOriginated)
        {
            return AggregateId.FromDatabaseGuid(
                SharedModule.Constants.MemoryAggregateIds.User
            );
        }

        if (SessionId == Guid.Empty)
        {
            throw new InvalidOperationException("A session ID is required.");
        }

        var sessionMap = await stateMachineHandler.GetByAggregateId(
            MemoryAggregateIds.SessionAggregateMap
        );
        var threadId = new ThreadId(SessionId);
        if (
            sessionMap?.StateData is not SessionAggregateMapStateData state
            || !state.AggregateIdsBySession.TryGetValue(
                threadId,
                out var memoryAggregateId
            )
        )
        {
            throw new InvalidOperationException(
                $"No memory exists for session '{SessionId}'."
            );
        }

        if (
            MemoryAggregateId != Guid.Empty
            && MemoryAggregateId != memoryAggregateId.Value
        )
        {
            throw new InvalidOperationException(
                $"Memory aggregate '{MemoryAggregateId}' does not belong to session '{SessionId}'."
            );
        }

        return MemoryAggregateId == Guid.Empty
            ? memoryAggregateId
            : AggregateId.FromDatabaseGuid(MemoryAggregateId);
    }

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

    protected async Task<object> ExecuteEvent(
        Executor executor,
        AggregateId featureId,
        IEvent eventData,
        AggregateId memoryAggregateId
    )
    {
        if (_isUserOriginated)
        {
            return await ExecuteEvent(executor, featureId, eventData);
        }

        var featurePayload = EventPayload.Create(
            executor.Id,
            featureId,
            StateMachineId,
            eventData
        );
        var relationPayload = EventPayload.Create(
            executor.Id,
            memoryAggregateId,
            Constants.StateMachineIds.Memory,
            new MemoryRelationAddedV1(eventData.GetType().Name, featureId)
        );
        await stateMachineHandler.ExecuteEvents(
            new List<EventPayload> { featurePayload, relationPayload }
        );
        return FeatureCommandResult.Ok;
    }
}

public abstract class ExistingFeatureCommand(StateMachineHandler stateMachineHandler)
    : FeatureCommand(stateMachineHandler)
{
    public required Guid FeatureId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(FeatureId != Guid.Empty);

    protected Task<object> ExecuteEvent(Executor executor, IEvent eventData) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(FeatureId),
            eventData
        );

    protected Task<object> ExecuteEvent(
        Executor executor,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(FeatureId),
            eventData,
            memoryAggregateId
        );
}
