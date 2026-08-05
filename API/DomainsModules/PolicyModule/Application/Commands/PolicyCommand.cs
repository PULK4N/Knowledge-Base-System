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
        Func<StateInfo, List<EventPayload>> conditionalEventsMethod
    ) =>
        stateMachineHandler.ExecuteEvents(
            conditionalEvent,
            conditionalEventsMethod
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
}
