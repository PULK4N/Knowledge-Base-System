using ProjectModule.Shared;
using ActionModule.Shared.Models;
using EventSourcing.Core;
using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Commands;

/// <summary>Replaces how a linked skill takes part in the project's code review.</summary>
public sealed class UpdateProjectSkillCommand(
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

        return await ExecuteProjectEvent(
            executor,
            new ProjectSkillRelationUpdatedV1(
                AggregateId.FromDatabaseGuid(SkillId),
                UsedForReview,
                ReviewNotes.Trim(),
                ReviewLevel,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
