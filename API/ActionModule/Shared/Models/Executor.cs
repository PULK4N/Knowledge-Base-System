using EventSourcing.Shared.Models;

namespace ActionModule.Shared.Models;

public sealed class Executor
{
    public required EventExecutor Id { get; set; }

    /// <summary>The administrator may do everything, so <see cref="Permissions"/> stays empty for it.</summary>
    public bool IsAdministrator { get; init; }

    /// <summary>Permissions such as <c>skills.write</c>, granted to an agent account.</summary>
    public HashSet<string> Permissions { get; init; } = [];

    /// <summary>True for the administrator and for a granted permission.</summary>
    public bool Has(string permission) =>
        IsAdministrator || Permissions.Contains(permission);
}
