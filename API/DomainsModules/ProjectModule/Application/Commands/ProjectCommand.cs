using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SharedModule.Constants;

namespace ProjectModule.Application.Commands;

public abstract class ProjectCommand(
    StateMachineHandler stateMachineHandler
) : Command<object>
{
    private bool _isUserOriginated;

    public void UseUserOrigin()
    {
        _isUserOriginated = true;
        SessionId = Guid.Empty;
        MemoryAggregateId = SharedModule.Constants.MemoryAggregateIds.User;
    }

    public Guid SessionId { get; set; }
    public Guid MemoryAggregateId { get; set; }

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
            MemoryModule.Domain.MemoryAggregateIds.SessionAggregateMap
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

    protected static AggregateId RepositoryToProjectMapAggregateId =>
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.RepositoryToProjectMap
        );

    protected static Policy CreatePolicy(
        PolicyId policyId,
        string title,
        string description
    ) =>
        new()
        {
            PolicyId = policyId,
            Title = title,
            Description = description
        };

    protected static EventPayload CreatePayload(
        Executor executor,
        AggregateId aggregateId,
        string stateMachineId,
        IEvent eventData
    ) =>
        EventPayload.Create(
            executor.Id,
            aggregateId,
            stateMachineId,
            eventData
        );

    protected Task ExecuteEvents(List<EventPayload> payloads) =>
        stateMachineHandler.ExecuteEvents(payloads);

    protected async Task<object> ExecuteProjectEvent(
        Executor executor,
        Guid projectId,
        IEvent eventData,
        AggregateId memoryAggregateId
    )
    {
        var payload = CreatePayload(
            executor,
            AggregateId.FromDatabaseGuid(projectId),
            Constants.StateMachineIds.Project,
            eventData
        );
        var payloads = new List<EventPayload> { payload };
        AddMemoryRelation(payloads, executor, payload, memoryAggregateId);
        await ExecuteEvents(payloads);
        return PolicyCommandResult.Ok;
    }

    protected void AddMemoryRelation(
        List<EventPayload> payloads,
        Executor executor,
        EventPayload projectPayload,
        AggregateId memoryAggregateId
    )
    {
        if (_isUserOriginated)
            return;

        payloads.Add(CreatePayload(
            executor,
            memoryAggregateId,
            MemoryModule.Application.Constants.StateMachineIds.Memory,
            new MemoryRelationAddedV1(
                projectPayload.EventData.GetType().Name,
                projectPayload.EventExecutionInfo.AggregateId
            )
        ));
    }

    protected Task<Dictionary<AggregateId, StateInfo>> ExecuteEvents(
        Executor executor,
        EventPayload conditionalEvent,
        Func<StateInfo[], List<EventPayload>> conditionalEventsMethod,
        AggregateId memoryAggregateId
    ) => stateMachineHandler.ExecuteEvents(
        conditionalEvent,
        stateInfos =>
        {
            var payloads = conditionalEventsMethod(stateInfos);
            AddMemoryRelation(payloads, executor, conditionalEvent, memoryAggregateId);
            return payloads;
        }
    );
}
