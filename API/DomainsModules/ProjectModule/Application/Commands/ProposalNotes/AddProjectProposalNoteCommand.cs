using ActionModule.Shared.Models;
using EventSourcing.Core;
using ProjectModule.Application.Models;
using ProjectModule.Domain.Events;
using ProjectModule.Domain.Models;

namespace ProjectModule.Application.Commands;

public sealed class AddProjectProposalNoteCommand(
    StateMachineHandler stateMachineHandler
) : ExistingProjectCommand(stateMachineHandler)
{
    public required string Title { get; set; }
    public string Content { get; set; } = string.Empty;

    protected override async Task<object> ExecuteInternal(Executor executor)
    {
        var memoryAggregateId = await ResolveMemoryAggregateId();
        var noteId = ProposalNoteId.New();

        await ExecuteProjectEvent(
            executor,
            new ProjectProposalNoteAddedV1(
                noteId,
                Title.Trim(),
                Content,
                SessionId,
                memoryAggregateId
            ),
            memoryAggregateId
        );

        return ProposalNoteAddedCommandResult.Ok(noteId.Value);
    }
}
