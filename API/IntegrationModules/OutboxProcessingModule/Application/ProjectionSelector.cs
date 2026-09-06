using EventSourcing.Core.Interfaces;
using EventSourcing.Shared.Interfaces;
using EventSourcing.Shared.Models;

namespace OutboxProcessingModule.Application;

/// <summary>
/// Reproduces the projector selection <see cref="EventSourcing.Core.ProjectionOutbox"/>
/// performs, at consume time. Selection comes from YAML rather than the message,
/// so a redelivery after a definition fix picks up the corrected set.
/// </summary>
public sealed class ProjectionSelector(
    IStateMachineDefinitionProvider _definitions,
    ProjectorRegistry _projectors
)
{
    public List<string> SelectNames(EventExecutionInfo info)
    {
        var definition = _definitions.Get(info.StateMachineId);
        definition.Events.TryGetValue(info.EventName, out var eventDefinition);

        return definition.Projections
            .Concat(eventDefinition?.Projections ?? [ ])
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public List<IProjector> Select(EventExecutionInfo info) =>
        SelectNames(info).Select(_projectors.GetRequired).ToList();
}
