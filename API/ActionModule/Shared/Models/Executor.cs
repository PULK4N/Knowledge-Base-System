using EventSourcing.Shared.Models;

namespace ActionModule.Shared.Models;

public sealed class Executor
{
    private const string ReadSuffix = ".read";
    private const string WriteSuffix = ".write";

    public required EventExecutor Id { get; set; }

    /// <summary>The administrator may do everything, so <see cref="Permissions"/> stays empty for it.</summary>
    public bool IsAdministrator { get; init; }

    /// <summary>Permissions such as <c>skills.write</c>, granted to an agent account.</summary>
    public HashSet<string> Permissions { get; init; } = [];

    /// <summary>
    /// True for the administrator, for a granted permission, and for an area's <c>read</c> permission
    /// when the same area's <c>write</c> permission is granted.
    /// </summary>
    public bool Has(string permission) =>
        IsAdministrator
        || Permissions.Contains(permission)
        || (permission.EndsWith(ReadSuffix, StringComparison.Ordinal)
            && Permissions.Contains(permission[..^ReadSuffix.Length] + WriteSuffix));
}
