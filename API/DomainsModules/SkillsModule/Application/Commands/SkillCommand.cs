using ActionModule;
using EventSourcing.Core;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;
using SkillsModule.Application.Models;

namespace SkillsModule.Application.Commands;

public abstract class SkillCommand(StateMachineHandler stateMachineHandler) : Command<object>
{
    protected const string StateMachineId = "skills-state-machine";

    public required Guid ExecutorId { get; init; }
    public required Guid SkillId { get; init; }

    public override Task<bool> IsAuthorized() => Task.FromResult(true);

    public override Task<bool> CanExecute() => Task.FromResult(true);

    protected async Task<object> ExecuteEvent(IEvent eventData)
    {
        var payload = EventPayload.Create(
            EventExecutor.FromDatabaseGuid(ExecutorId),
            AggregateId.FromDatabaseGuid(SkillId),
            StateMachineId,
            eventData
        );

        await stateMachineHandler.ExecuteEvents(payload);

        return SkillCommandResult.Ok;
    }
}
