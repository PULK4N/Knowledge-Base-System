using EventSourcing.Persistence.Models;

namespace OutboxProcessingModule.Application;

/// <summary>
/// A claimed outbox row together with every queue it must reach. An empty
/// queue list means the row is finished without a broker send.
/// </summary>
public sealed record OutboxDispatch(
    SerializedPayloadMessage Row,
    List<string> Queues
);
