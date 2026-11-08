using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using SkillsModule.Domain.Events;

namespace ProjectModule.Application.Commands;

public sealed class RemoveSkillFromProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty && SkillId != Guid.Empty);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var projectId = AggregateId.FromDatabaseGuid(ProjectId);
        var skillId = AggregateId.FromDatabaseGuid(SkillId);
        var projectPayload = CreatePayload(
            executor,
            projectId,
            Constants.StateMachineIds.Project,
            new ProjectSkillRelationRemovedV1(
                skillId,
                SessionId,
                memoryAggregateId
            )
        );
        var payloads = new List<EventPayload>
        {
            projectPayload,
            CreatePayload(
                executor,
                skillId,
                Constants.StateMachineIds.Skills,
                new SkillProjectRelationRemovedV1(
                    projectId,
                    SessionId,
                    memoryAggregateId
                )
            )
        };
        AddMemoryRelation(payloads, executor, projectPayload, memoryAggregateId);

        await ExecuteEvents(payloads);
        return PolicyCommandResult.Ok;
    }
}
