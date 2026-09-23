using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using Constants = MemoryModule.Application.Constants;
using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using SkillsModule.Application.Models;

namespace SkillsModule.Application.Commands;

public abstract class SkillCommand(StateMachineHandler stateMachineHandler)
    : Command<object>
{
    protected const string StateMachineId = "skills-state-machine";

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
        AggregateId skillId,
        IEvent eventData
    )
    {
        var payload = EventPayload.Create(
            executor.Id,
            skillId,
            StateMachineId,
            eventData
        );
        await stateMachineHandler.ExecuteEvents(payload);
        return SkillCommandResult.Ok;
    }

    protected async Task<object> ExecuteEvent(
        Executor executor,
        AggregateId skillId,
        IEvent eventData,
        AggregateId memoryAggregateId
    )
    {
        if (_isUserOriginated)
        {
            return await ExecuteEvent(executor, skillId, eventData);
        }

        var skillPayload = EventPayload.Create(
            executor.Id,
            skillId,
            StateMachineId,
            eventData
        );
        var relationPayload = EventPayload.Create(
            executor.Id,
            memoryAggregateId,
            Constants.StateMachineIds.Memory,
            new MemoryRelationAddedV1(eventData.GetType().Name, skillId)
        );
        await stateMachineHandler.ExecuteEvents(
            new List<EventPayload> { skillPayload, relationPayload }
        );
        return SkillCommandResult.Ok;
    }
}

public abstract class ExistingSkillCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }

    protected Task<object> ExecuteEvent(Executor executor, IEvent eventData) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(SkillId),
            eventData
        );

    protected Task<object> ExecuteEvent(
        Executor executor,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(SkillId),
            eventData,
            memoryAggregateId
        );
}
