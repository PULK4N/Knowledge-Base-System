using Apache.NMS;
using SharedModule.DistributedMessaging.Consuming;

namespace OutboxProcessingModule.Hosting;

/// <summary>
/// Each subscription owns its connection, so one queue's broker failure never
/// disturbs another queue's consumer or the publisher. The connection comes
/// from the shared factory, so consumers get the same failover reconnect as
/// the publisher: a dropped socket is re-established and the consumer link
/// re-created by the client, and the worker only sees a transaction it must
/// roll back and receive again.
/// </summary>
public sealed class NmsQueueSubscriptionFactory(
    BrokerConnectionFactory _factory
) : IQueueSubscriptionFactory
{
    public Task<IQueueSubscription> Subscribe(
        string queueName, CancellationToken cancellationToken)
    {
        var connection = _factory.Create().CreateConnection();

        try
        {
            var session = connection.CreateSession(AcknowledgementMode.Transactional);
            var consumer = session.CreateConsumer(session.GetQueue(queueName));
            connection.Start();

            return Task.FromResult<IQueueSubscription>(
                new NmsQueueSubscription(connection, session, consumer));
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}

public sealed class NmsQueueSubscription(
    IConnection _connection,
    ISession _session,
    IMessageConsumer _consumer
) : IQueueSubscription
{
    public Task<string?> Receive(TimeSpan timeout, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var message = _consumer.Receive(timeout);

        return message switch
        {
            null => Task.FromResult<string?>(null),
            ITextMessage text => Task.FromResult<string?>(text.Text),
            _ => throw new InvalidOperationException(
                $"Expected a JSON text message but got {message.GetType().Name}.")
        };
    }

    public Task Acknowledge(CancellationToken cancellationToken) =>
        _session.CommitAsync();

    public Task Redeliver(CancellationToken cancellationToken) =>
        _session.RollbackAsync();

    public void Dispose()
    {
        _consumer.Dispose();
        _session.Dispose();
        _connection.Dispose();
    }
}
