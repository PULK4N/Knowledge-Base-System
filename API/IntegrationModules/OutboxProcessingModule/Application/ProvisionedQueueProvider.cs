using EventSourcing.Core.Interfaces;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Application;

/// <summary>
/// The queues the broker must hold, derived from the state machine definitions.
/// A state machine that declares no projections and no hooks gets no queue and
/// its rows are marked Sent without publishing.
/// </summary>
public sealed class ProvisionedQueueProvider(
    IStateMachineDefinitionProvider _definitions
)
{
    public List<ProvisionedQueue> GetAll()
    {
        var queues = new List<ProvisionedQueue>();

        foreach (var definition in _definitions.GetAll())
        {
            var declaresProjections =
                definition.Projections.Count > 0
                || definition.Events.Values.Any(
                    eventDefinition => eventDefinition.Projections.Count > 0);

            var declaresHooks = definition.Events.Values.Any(
                eventDefinition => eventDefinition.Hooks.Count > 0);

            if (declaresProjections)
                queues.Add(new ProvisionedQueue(
                    definition.Id, DeliveryRole.Projections));

            if (declaresHooks)
                queues.Add(new ProvisionedQueue(definition.Id, DeliveryRole.Hooks));
        }

        return queues;
    }
}
