using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace IdentityModule.Application.SessionKeys;

/// <summary>
/// Reads the session keys agents send as <c>Authorization: Bearer</c>. A key is the agent's account ID,
/// encrypted and stamped with the Data Protection secret, so only this API can make one that reads back.
/// </summary>
public sealed class SessionKeyProtector(IDataProtectionProvider provider)
{
    /// <summary>
    /// Mixed into the secret, so values made for another purpose, such as the login cookie, do not read
    /// as keys. Changing it makes every issued key unreadable.
    /// </summary>
    public const string Purpose = "KnowledgeBase.SessionKey.v1";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    /// <summary>
    /// Makes a key for the account. Nothing is stored: the key itself carries the account ID.
    /// </summary>
    public string Protect(Guid accountId) => _protector.Protect(accountId.ToString());

    /// <summary>
    /// Reads the account ID inside the key. Returns <c>false</c> when the key was not made by this API, was
    /// changed, or was made for another purpose.
    /// </summary>
    public bool TryUnprotect(string sessionKey, out Guid accountId)
    {
        try
        {
            return Guid.TryParse(_protector.Unprotect(sessionKey), out accountId);
        }
        catch (CryptographicException)
        {
            accountId = Guid.Empty;
            return false;
        }
    }
}
