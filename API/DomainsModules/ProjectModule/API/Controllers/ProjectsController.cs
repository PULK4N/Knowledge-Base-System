using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using ProjectModule.Application.DTOs;
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
}
