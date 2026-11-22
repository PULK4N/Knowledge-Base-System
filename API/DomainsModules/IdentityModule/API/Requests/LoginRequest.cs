using System.ComponentModel.DataAnnotations;

namespace IdentityModule.API.Requests;

public sealed record LoginRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}
