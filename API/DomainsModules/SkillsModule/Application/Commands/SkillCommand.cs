using ActionModule;
using ActionModule.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Application.Models;

namespace SkillsModule.Application.Commands;

public abstract class SkillCommand(StateMachineHandler stateMachineHandler) : Command<object>
{
    protected const string StateMachineId = "skills-state-machine";

    public override Task<bool> IsAuthorized(Executor executor) =>
        Task.FromResult(true);

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(true);

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
}

public abstract class ExistingSkillCommand(
    StateMachineHandler stateMachineHandler
) : SkillCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }

    protected Task<object> ExecuteEvent(
        Executor executor,
        IEvent eventData
    ) =>
        ExecuteEvent(
            executor,
            AggregateId.FromDatabaseGuid(SkillId),
            eventData
        );
}
