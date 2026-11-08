using ActionModule.Shared.Models;
using EventSourcing.Core;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Commands;

public sealed class RemoveProjectProposalNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required Guid NoteId { get; set; }

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty && NoteId != Guid.Empty);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectEvent(
            executor,
            new ProjectProposalNoteRemovedV1(
                ProposalNoteId.FromDatabaseGuid(NoteId),
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
