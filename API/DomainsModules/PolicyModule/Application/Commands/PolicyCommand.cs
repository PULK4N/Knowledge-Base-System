using MemoryModule.Domain;
using MemoryModule.Domain.Events;
using MemoryModule.Domain.Models;
using ActionModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using PolicyModule.Application.Models;
using PolicyModule.Domain.Models;
using SharedModule.Constants;

namespace PolicyModule.Application.Commands;

public abstract class PolicyCommand(
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

    protected static AggregateId GeneralPoliciesAggregateId =>
        AggregateId.FromDatabaseGuid(
            StateDataAggregateIds.GeneralPolicies
        );

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

    protected async Task<object> ExecuteEvent(
        Executor executor,
        AggregateId aggregateId,
        string stateMachineId,
        IEvent eventData
    )
    {
        await stateMachineHandler.ExecuteEvents(
            CreatePayload(
                executor,
                aggregateId,
                stateMachineId,
                eventData
            )
        );

        return PolicyCommandResult.Ok;
    }

    protected Task<object> ExecuteGeneralPoliciesEvent(
        Executor executor,
        IEvent eventData
    ) =>
        ExecuteEvent(
            executor,
            GeneralPoliciesAggregateId,
            Constants.StateMachineIds.GeneralPolicies,
            eventData
        );

    protected Task<object> ExecuteProjectPoliciesEvent(
        Executor executor,
        Guid projectId,
        IEvent eventData
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(projectId),
            Constants.StateMachineIds.ProjectPolicies,
            eventData
        );

    protected Task ExecuteEvents(List<EventPayload> payloads) =>
        stateMachineHandler.ExecuteEvents(payloads);

    protected Task<Dictionary<AggregateId, StateInfo>> ExecuteEvents(
        EventPayload conditionalEvent,
        Func<StateInfo[], List<EventPayload>> conditionalEventsMethod
    ) =>
        stateMachineHandler.ExecuteEvents(
            conditionalEvent,
            conditionalEventsMethod
        );

    protected async Task<object> ExecuteEvent(
        Executor executor,
        AggregateId aggregateId,
        string stateMachineId,
        IEvent eventData,
        AggregateId memoryAggregateId
    )
    {
        var payload = CreatePayload(executor, aggregateId, stateMachineId, eventData);
        var payloads = new List<EventPayload> { payload };
        AddMemoryRelation(payloads, executor, payload, memoryAggregateId);
        await ExecuteEvents(payloads);
        return PolicyCommandResult.Ok;
    }

    protected Task<object> ExecuteGeneralPoliciesEvent(
        Executor executor,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteEvent(
            executor,
            GeneralPoliciesAggregateId,
            Constants.StateMachineIds.GeneralPolicies,
            eventData,
            memoryAggregateId
        );

    protected Task<object> ExecuteProjectPoliciesEvent(
        Executor executor,
        Guid projectId,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(projectId),
            Constants.StateMachineIds.ProjectPolicies,
            eventData,
            memoryAggregateId
        );

    protected void AddMemoryRelation(
        List<EventPayload> payloads,
        Executor executor,
        EventPayload policyPayload,
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
                policyPayload.EventData.GetType().Name,
                policyPayload.EventExecutionInfo.AggregateId
            )
        ));
    }

    protected Task<Dictionary<AggregateId, StateInfo>> ExecuteEvents(
        Executor executor,
        EventPayload conditionalEvent,
        Func<StateInfo[], List<EventPayload>> conditionalEventsMethod,
        AggregateId memoryAggregateId
    ) => ExecuteEvents(
        conditionalEvent,
        stateInfos =>
        {
            var payloads = conditionalEventsMethod(stateInfos);
            AddMemoryRelation(payloads, executor, conditionalEvent, memoryAggregateId);
            return payloads;
        }
    );
}

public abstract class ExistingProjectPoliciesCommand(
    StateMachineHandler stateMachineHandler
) : PolicyCommand(stateMachineHandler)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected Task<object> ExecuteProjectPoliciesEvent(
        Executor executor,
        IEvent eventData
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            ProjectId,
            eventData
        );

    protected Task<object> ExecuteProjectPoliciesEvent(
        Executor executor,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteProjectPoliciesEvent(
            executor,
            ProjectId,
            eventData,
            memoryAggregateId
        );

}
