using ActionModule.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Settings;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(SettingsRepository repository)
    : ControllerBase
{
    // The login page reads the theme before anyone is logged in.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<Settings>> Get(
        CancellationToken cancellationToken
    )
    {
        try
        {
            return Ok(await repository.Get(cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // Not an action, so the administrator check that actions make in IsAuthorized is made here.
    [HttpPut]
    public async Task<ActionResult<Settings>> Update(
        [FromBody] SettingsRequest request,
        [FromServices] IExecutorProvider executorProvider,
        CancellationToken cancellationToken
    )
    {
        if (!(await executorProvider.GetExecutor()).IsAdministrator)
            return StatusCode(StatusCodes.Status403Forbidden);

        try
        {
            return Ok(
                await repository.Update(request.Theme, cancellationToken)
            );
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

public sealed record SettingsRequest
{
    public required Theme Theme { get; init; }
}
