using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using SkillsModule.Domain.Events;

namespace SkillsModule.Application.Commands;

public sealed class DeleteSkillCommand(StateMachineHandler stateMachineHandler)
    : ExistingSkillCommand(stateMachineHandler)
{
    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var skillId = AggregateId.FromDatabaseGuid(SkillId);

        return await ExecuteEvents(
            executor,
            skillId,
            new SkillDeletedV2(SessionId, memoryAggregateId),
            skill => skill.RelatedProjectIds
                .Select(
                    projectId => EventPayload.Create(
                        executor.Id,
                        projectId,
                        ProjectStateMachineId,
                        new ProjectSkillRelationRemovedV1(
                            skillId,
                            SessionId,
                            memoryAggregateId
                        )
                    )
                )
                .ToList(),
            memoryAggregateId
        );
    }
}
