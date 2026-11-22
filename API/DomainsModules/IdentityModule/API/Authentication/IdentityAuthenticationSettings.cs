namespace IdentityModule.API.Authentication;

/// <summary>
/// The <c>Authentication</c> section of appsettings, also settable as <c>Authentication__Enabled</c>.
/// </summary>
public sealed class IdentityAuthenticationSettings
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// When <c>false</c>, no endpoint requires a login and requests without one run as the administrator.
    /// A cookie or session key that is sent still identifies its account. Meant for a machine where login
    /// is not set up yet or is broken.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
