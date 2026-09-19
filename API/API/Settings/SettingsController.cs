using Microsoft.AspNetCore.Mvc;

namespace Api.Settings;

[ApiController]
[Route("api/settings")]
public sealed class SettingsController(SettingsRepository repository)
    : ControllerBase
{
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

    [HttpPut]
    public async Task<ActionResult<Settings>> Update(
        [FromBody] SettingsRequest request,
        CancellationToken cancellationToken
    )
    {
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
