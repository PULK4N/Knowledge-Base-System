using System.ComponentModel;
using Microsoft.Extensions.AI;
using ProjectModule.Application.Commands;
using ProjectModule.Application.DTOs;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;

namespace ProjectModule.MCP;

internal static class ProjectProposalNoteMcpFunctions
{
    public static List<AIFunction> Create() =>
        [
            ProjectMcpFunctions.Create(
                ListNotes,
                "project_proposal_note_list",
                "Lists a project's proposal notes, newest first: ideas written down to implement later."
            ),
            ProjectMcpFunctions.Create(
                AddNote,
                "project_proposal_note_add",
                "Adds a proposal note to a project: an idea to implement later, with a short title and Markdown content."
            ),
            ProjectMcpFunctions.Create(
                UpdateNote,
                "project_proposal_note_update",
                "Replaces a proposal note's title and Markdown content."
            ),
            ProjectMcpFunctions.Create(
                RemoveNote,
                "project_proposal_note_remove",
                "Removes a proposal note from a project."
            )
        ];

    private static async Task<List<ProposalNoteDto>?> ListNotes(
        IServiceProvider services,
        Guid projectId
    ) =>
        (
            await ProjectMcpActionExecutor.ExecuteQuery<
                GetPolicyProjectQuery,
                PolicyProjectDetailsDto?
            >(services, query => query.ProjectId = projectId)
        )?.ProposalNotes;

    private static Task<ProposalNoteAddedCommandResult> AddNote(
        IServiceProvider services,
        Guid projectId,
        string title,
        [Description("Markdown content of the idea.")]
        string content,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            AddProjectProposalNoteCommand,
            ProposalNoteAddedCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.Title = title;
                command.Content = content;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> UpdateNote(
        IServiceProvider services,
        Guid projectId,
        Guid noteId,
        string title,
        [Description("Markdown content of the idea.")]
        string content,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            UpdateProjectProposalNoteCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.NoteId = noteId;
                command.Title = title;
                command.Content = content;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );

    private static Task<PolicyCommandResult> RemoveNote(
        IServiceProvider services,
        Guid projectId,
        Guid noteId,
        Guid? sessionId = null,
        Guid? memoryAggregateId = null
    ) =>
        ProjectMcpActionExecutor.ExecuteCommand<
            RemoveProjectProposalNoteCommand,
            PolicyCommandResult
        >(
            services,
            command =>
            {
                command.ProjectId = projectId;
                command.NoteId = noteId;
                command.SessionId = sessionId.GetValueOrDefault();
                command.MemoryAggregateId = memoryAggregateId.GetValueOrDefault();
            }
        );
}
