using System.ComponentModel.DataAnnotations;

namespace IdentityModule.API.Requests;

public sealed record CreateAgentRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;

    public string? DisplayName { get; init; }

    public bool IsAdministrator { get; init; }

    public List<string> Permissions { get; init; } = [];
}
