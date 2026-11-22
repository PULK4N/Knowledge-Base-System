using ActionModule.API;
using ActionModule.Shared;
using IdentityModule.API.Requests;
using IdentityModule.Application.Commands;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace IdentityModule.API.Controllers;

/// <summary>
/// Administrator login and password changes. Credentials are checked here, but no session cookie is
/// issued yet.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IExecutorProvider executorProvider) : ActionController(executorProvider)
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginQuery query)
    {
        query.UserName = request.UserName;
        query.Password = request.Password;
        var result = await Execute(query);
        return result.Succeeded ? Ok(result) : Unauthorized();
    }

    [HttpPost("password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ChangeAdministratorPasswordCommand command)
    {
        command.CurrentPassword = request.CurrentPassword;
        command.NewPassword = request.NewPassword;
        command.ConfirmPassword = request.ConfirmPassword;
        try
        {
            await Execute(command);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }
}
