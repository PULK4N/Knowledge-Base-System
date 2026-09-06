using EventSourcing.Persistence.Models;

namespace OutboxProcessingModule.Application;

/// <summary>
/// The claimed rows are the unit of work. They stay tracked by the scoped
/// DbContext between the three calls, so Complete and Fail mutate the same
/// instances Claim returned and never re-query.
/// </summary>
public interface IOutboxDispatchRepository
{
    Task<List<SerializedPayloadMessage>> Claim(
        int limit, CancellationToken cancellationToken);

    Task Complete(
        List<SerializedPayloadMessage> rows, CancellationToken cancellationToken);

    Task Fail(
        List<SerializedPayloadMessage> rows, string error,
        CancellationToken cancellationToken);
}
