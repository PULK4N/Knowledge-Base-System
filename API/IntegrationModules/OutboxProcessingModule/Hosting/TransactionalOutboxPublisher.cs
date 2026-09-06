using System.Text.Json;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// Sends every message of a claimed set on one transactional session and
/// commits once. A partial publish would run some handlers and silently drop
/// others, so there is no per-message commit and no parallel send.
/// </summary>
public sealed class TransactionalOutboxPublisher(
    NmsConnectionManager _connections,
    ILogger<TransactionalOutboxPublisher> _logger
) : IOutboxPublisher
{
    public async Task Publish(
        List<OutboxDispatch> dispatches, CancellationToken cancellationToken)
    {
        var connection = await _connections.Get(cancellationToken);
        using var session = connection.CreateSession(AcknowledgementMode.Transactional);

        try
        {
            foreach (var dispatch in dispatches)
            {
                if (dispatch.Queues.Count == 0)
                    continue;

                // The stored row is the message. Copying it never re-serializes
                // the event, so dispatch cannot change the event schema.
                var body = JsonSerializer.Serialize(dispatch.Row);

                foreach (var queueName in dispatch.Queues)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using var producer = session.CreateProducer(
                        session.GetQueue(queueName));
                    producer.DeliveryMode = MsgDeliveryMode.Persistent;

                    await producer.SendAsync(session.CreateTextMessage(body));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await session.CommitAsync();
        }
        catch (Exception publishError)
        {
            try
            {
                await session.RollbackAsync();
            }
            catch (Exception rollbackError)
            {
                _logger.LogWarning(
                    rollbackError, "Rolling back the dispatch transaction failed.");
            }

            // The connection may be broken, and a failed commit response can
            // also mean the outcome is unknown.
            _connections.Reset();
            throw new BatchPublicationException(publishError);
        }
    }
}
