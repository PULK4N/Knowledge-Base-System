using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillCommand(StateMachineHandler stateMachineHandler)
    : SkillCommand(stateMachineHandler)
{
    protected override Task<object> ExecuteInternal() =>
        ExecuteEvent(new SkillDeleted());
}
