namespace OutboxProcessingModule.Application;

public interface IOutboxPublisher
{
    /// <summary>
    /// Sends every message of the claimed set in one broker transaction, so the
    /// set is committed whole or not at all.
    /// </summary>
    Task Publish(List<OutboxDispatch> dispatches, CancellationToken cancellationToken);
}
