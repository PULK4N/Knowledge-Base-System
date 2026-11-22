using ActionModule.API;
using ActionModule.Shared;
using IdentityModule.API.Authentication;
using IdentityModule.API.Requests;
using IdentityModule.Application;
using IdentityModule.Application.Commands;
using IdentityModule.Application.DTOs;
using IdentityModule.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace IdentityModule.API.Controllers;

/// <summary>
/// Administrator login, logout and password changes. Only the administrator logs in with a password,
/// so the login cookie is always issued for the administrator account.
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
        if (!result.Succeeded)
            return Unauthorized();

        // The initial password must be changed first; the password change then issues the cookie.
        if (!result.MustChangePassword)
            await HttpContext.SignInAccount(IdentityDefaults.AdministratorId.Value);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAccount();
        return NoContent();
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
            await HttpContext.SignInAccount(IdentityDefaults.AdministratorId.Value);
            return NoContent();
        }
        catch (InvalidOperationException)
        {
            return BadRequest();
        }
    }
}
