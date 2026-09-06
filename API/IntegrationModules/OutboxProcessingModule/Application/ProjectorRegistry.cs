using EventSourcing.Shared.Interfaces;

namespace OutboxProcessingModule.Application;

/// <summary>
/// Resolves a projector by the class name a state machine definition uses.
/// Registered scoped, because the projectors it holds are scoped.
/// </summary>
public sealed class ProjectorRegistry
{
    private readonly IReadOnlyDictionary<string, IProjector> _projectors;

    public ProjectorRegistry(IEnumerable<IProjector> projectors)
    {
        var byName = new Dictionary<string, IProjector>(StringComparer.Ordinal);

        foreach (var projector in projectors)
        {
            var projectorName = projector.GetType().Name;

            if (!byName.TryAdd(projectorName, projector))
                throw new InvalidOperationException(
                    $"Multiple projectors are registered with name '{projectorName}'."
                );
        }

        _projectors = byName;
    }

    public IProjector GetRequired(string projectorName) =>
        _projectors.TryGetValue(projectorName, out var projector)
            ? projector
            : throw new InvalidOperationException(
                $"Projector '{projectorName}' is named by a state machine "
                + "definition but is not registered."
            );
}
