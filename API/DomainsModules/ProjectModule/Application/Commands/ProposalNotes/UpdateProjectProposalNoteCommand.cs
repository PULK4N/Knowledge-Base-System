using ActionModule.Shared.Models;
using EventSourcing.Core;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Commands;

public sealed class UpdateProjectProposalNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required Guid NoteId { get; set; }
    public required string Title { get; set; }
    public string Content { get; set; } = string.Empty;

    public override Task<bool> CanExecute(Executor executor) =>
        Task.FromResult(ProjectId != Guid.Empty && NoteId != Guid.Empty);

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();

        return await ExecuteProjectEvent(
            executor,
            new ProjectProposalNoteUpdatedV1(
                ProposalNoteId.FromDatabaseGuid(NoteId),
                Title.Trim(),
                Content,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );
    }
}
