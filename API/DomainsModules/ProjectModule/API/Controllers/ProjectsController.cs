using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using ProjectModule.API.Requests;
using ProjectModule.Application.Commands;
using ProjectModule.Application.DTOs;
using ProjectModule.Application.Models;
using ProjectModule.Application.Queries;

namespace ProjectModule.API.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    IExecutorProvider executorProvider
) : ActionController(executorProvider)
{
    /// <summary>Review data a review script fetches: level models and the skills used for review.</summary>
    [HttpGet("{projectId:guid}/review")]
    public async Task<ActionResult<ProjectReviewDto>> GetReview(
        Guid projectId,
        [FromServices] GetProjectReviewQuery query
    )
    {
        query.ProjectId = projectId;
        var review = await Execute(query);

        return review is null ? NotFound() : Ok(review);
    }

    [HttpPost("{projectId:guid}/proposal-notes")]
    public async Task<ActionResult<ProposalNoteAddedCommandResult>> AddProposalNote(
        Guid projectId,
        [FromBody] ProposalNoteRequest request,
        [FromServices] AddProjectProposalNoteCommand command
    )
    {
        command.ProjectId = projectId;
        command.Title = request.Title;
        command.Content = request.Content ?? string.Empty;
        command.UseUserOrigin();
        return Ok((ProposalNoteAddedCommandResult)await Execute(command));
    }

    [HttpPost("{projectId:guid}/proposal-notes/{noteId:guid}/update")]
    public async Task<ActionResult<PolicyCommandResult>> UpdateProposalNote(
        Guid projectId,
        Guid noteId,
        [FromBody] ProposalNoteRequest request,
        [FromServices] UpdateProjectProposalNoteCommand command
    )
    {
        command.ProjectId = projectId;
        command.NoteId = noteId;
        command.Title = request.Title;
        command.Content = request.Content ?? string.Empty;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }

    [HttpPost("{projectId:guid}/proposal-notes/{noteId:guid}/remove")]
    public async Task<ActionResult<PolicyCommandResult>> RemoveProposalNote(
        Guid projectId,
        Guid noteId,
        [FromServices] RemoveProjectProposalNoteCommand command
    )
    {
        command.ProjectId = projectId;
        command.NoteId = noteId;
        command.UseUserOrigin();
        return Ok((PolicyCommandResult)await Execute(command));
    }
}
