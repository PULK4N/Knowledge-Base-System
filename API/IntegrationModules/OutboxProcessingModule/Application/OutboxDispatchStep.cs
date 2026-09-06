using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OutboxProcessingModule.Application;

/// <summary>
/// One publisher cycle: claim rows, route them, publish them in one broker
/// transaction, then mark them Sent. A row is Sent only after the broker
/// acknowledged the commit, so Sent means published, not processed.
/// </summary>
public sealed class OutboxDispatchStep(
    IOutboxDispatchRepository _repository,
    IOutboxQueueResolver _resolver,
    IOutboxPublisher _publisher,
    IOptions<OutboxProcessingOptions> _options,
    ILogger<OutboxDispatchStep> _logger
)
{
    /// <returns>True when rows were claimed, so the caller can loop without waiting.</returns>
    public async Task<bool> Execute(CancellationToken cancellationToken)
    {
        var settings = _options.Value;

        var rows = await _repository.Claim(settings.BatchSize, cancellationToken);
        if (rows.Count == 0)
            return false;

        try
        {
            // Routing runs before any send, so an unknown state machine fails
            // the whole set instead of publishing part of it.
            var dispatches = _resolver.Resolve(rows);

            if (dispatches.Any(dispatch => dispatch.Queues.Count > 0))
                await _publisher.Publish(dispatches, cancellationToken);

            await _repository.Complete(rows, cancellationToken);
            return true;
        }
        catch (Exception error)
        {
            _logger.LogError(
                error, "Dispatching {RowCount} outbox rows failed.", rows.Count);

            // An independent token, so a shutdown still records the failure.
            using var cleanup = new CancellationTokenSource(settings.CleanupTimeout);
            await _repository.Fail(rows, error.ToString(), cleanup.Token);
            throw;
        }
    }
}
