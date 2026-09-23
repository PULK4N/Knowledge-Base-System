using ActionModule.Shared.Models;
using EventSourcing.Core;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillCommand(StateMachineHandler stateMachineHandler)
    : ExistingSkillCommand(stateMachineHandler)
{
    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        return await ExecuteEvent(
            executor,
            new SkillDeletedV2(SessionId, memoryAggregateId),
            memoryAggregateId
        );
    }
}
