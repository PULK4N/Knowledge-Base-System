using ActionModule.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillCommand(StateMachineHandler stateMachineHandler)
    : ExistingSkillCommand(stateMachineHandler)
{
    protected override Task<object> ExecuteInternal(Executor executor) =>
        ExecuteEvent(executor, new SkillDeleted());
}
