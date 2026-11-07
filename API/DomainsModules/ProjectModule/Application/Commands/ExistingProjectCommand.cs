using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace ProjectModule.Application.Commands;

public abstract class ExistingProjectCommand(
    StateMachineHandler stateMachineHandler
) : ProjectCommand(stateMachineHandler)
{
    public required Guid ProjectId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty);

    protected Task<object> ExecuteProjectEvent(
        Executor executor,
        IEvent eventData,
        AggregateId memoryAggregateId
    ) =>
        ExecuteProjectEvent(
            executor,
            ProjectId,
            eventData,
            memoryAggregateId
        );
}
