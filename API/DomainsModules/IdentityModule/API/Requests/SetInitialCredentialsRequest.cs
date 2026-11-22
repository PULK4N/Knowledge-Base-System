using System.ComponentModel.DataAnnotations;

namespace IdentityModule.API.Requests;

public sealed record SetInitialCredentialsRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    public string? NewUserName { get; init; }

    [Required]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    public string ConfirmPassword { get; init; } = string.Empty;
}
