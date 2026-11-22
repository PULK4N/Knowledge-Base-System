namespace IdentityModule.Application.DTOs;

public sealed record LoginResultDto(bool Succeeded, bool MustChangePassword)
{
    public static LoginResultDto Failed => new(false, false);
}
