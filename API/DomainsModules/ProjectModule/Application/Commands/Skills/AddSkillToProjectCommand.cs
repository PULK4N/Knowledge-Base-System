using ProjectModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;
using SkillsModule.Domain.Events;

namespace ProjectModule.Application.Commands;

/// <summary>
/// Links a skill to a project by writing the relation to both streams in one
/// write, so the validators of both sides run and the link is never one-sided.
/// </summary>
public sealed class AddSkillToProjectCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required Guid SkillId { get; set; }
    public bool UsedForReview { get; set; }
    public string ReviewNotes { get; set; } = string.Empty;
    public ReviewLevel? ReviewLevel { get; set; }

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
            new ProjectSkillRelationAddedV2(
                skillId,
                UsedForReview,
                ReviewNotes.Trim(),
                ReviewLevel,
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
                new SkillProjectRelationAddedV1(
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
