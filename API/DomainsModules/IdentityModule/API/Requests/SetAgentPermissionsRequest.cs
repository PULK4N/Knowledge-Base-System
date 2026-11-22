namespace IdentityModule.API.Requests;

public sealed record SetAgentPermissionsRequest
{
    public bool IsAdministrator { get; init; }

    public List<string> Permissions { get; init; } = [];
}
