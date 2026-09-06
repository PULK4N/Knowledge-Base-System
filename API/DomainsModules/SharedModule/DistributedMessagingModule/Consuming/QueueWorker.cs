using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedModule.DistributedMessaging.Queues;

namespace SharedModule.DistributedMessaging.Consuming;

/// <summary>
/// Consumes one queue, one delivery at a time, in a fresh DI scope per message.
/// A state machine's events reach one queue and are consumed in publish order,
/// which is what removes the need for ordering bookkeeping in the database.
/// </summary>
public sealed class QueueWorker(
    ProvisionedQueue _queue,
    IQueueSubscriptionFactory _subscriptions,
    IServiceScopeFactory _scopes,
    ConsumerSettings _settings,
    ILogger<QueueWorker> _logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = _settings.RetryDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var subscription = await _subscriptions.Subscribe(
                    _queue.Name, stoppingToken);

                retryDelay = _settings.RetryDelay;
                await Consume(subscription, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception error)
            {
                _logger.LogError(
                    error, "Consuming {Queue} stopped; reconnecting.", _queue.Name);

                // A broken session is never reused; the next loop builds a new one.
                await Wait(retryDelay, stoppingToken);
                retryDelay = Next(retryDelay, _settings.MaxRetryDelay);
            }
        }
    }

    private async Task Consume(
        IQueueSubscription subscription, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var body = await subscription.Receive(
                _settings.ReceiveTimeout, stoppingToken);

            if (body is null)
                continue;

            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                await Resolve(scope.ServiceProvider).Handle(body, stoppingToken);

                await subscription.Acknowledge(stoppingToken);
            }
            catch (Exception error)
            {
                _logger.LogError(error, "Delivery failed on {Queue}.", _queue.Name);

                await subscription.Redeliver(CancellationToken.None);

                if (stoppingToken.IsCancellationRequested)
                    break;
            }
        }
    }

    private IDeliveryHandler Resolve(IServiceProvider services)
    {
        var handlers = services.GetServices<IDeliveryHandler>()
            .Where(handler => handler.Role == _queue.Role)
            .ToList();

        return handlers.Count == 1
            ? handlers[0]
            : throw new InvalidOperationException(
                $"Queue '{_queue.Name}' needs exactly one {_queue.Role} handler "
                + $"but {handlers.Count} are registered.");
    }

    private static TimeSpan Next(TimeSpan current, TimeSpan maximum)
    {
        var doubled = current + current;
        return doubled > maximum ? maximum : doubled;
    }

    private static async Task Wait(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown; the loop condition ends the worker.
        }
    }
}
