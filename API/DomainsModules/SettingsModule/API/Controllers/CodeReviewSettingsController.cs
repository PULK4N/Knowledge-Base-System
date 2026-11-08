using ActionModule.API;
using ActionModule.Shared;
using Microsoft.AspNetCore.Mvc;
using SettingsModule.API.Requests;
using SettingsModule.Application.Commands;
using SettingsModule.Application.DTOs;
using SettingsModule.Application.Queries;

namespace SettingsModule.API.Controllers;

[ApiController]
[Route("api/settings/code-review")]
public sealed class CodeReviewSettingsController(IExecutorProvider executorProvider)
    : ActionController(executorProvider)
{
    [HttpGet]
    public async Task<ActionResult<CodeReviewSettingsDto>> Get(
        [FromServices] GetCodeReviewSettingsQuery query)
    {
        var settings = await Execute(query);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPost]
    public async Task<ActionResult<CodeReviewSettingsDto>> Update(
        [FromBody] UpdateCodeReviewSettingsRequest request,
        [FromServices] UpdateCodeReviewSettingsCommand command)
    {
        command.BatchSize = request.BatchSize;
        command.TaskMessage = request.TaskMessage;
        return Ok(await Execute(command));
    }
}
