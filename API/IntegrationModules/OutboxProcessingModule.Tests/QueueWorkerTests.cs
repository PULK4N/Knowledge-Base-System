using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SharedModule.DistributedMessaging.Consuming;
using SharedModule.DistributedMessaging.Queues;

namespace OutboxProcessingModule.Tests;

public sealed class QueueWorkerTests
{
    private static readonly ProvisionedQueue Queue =
        new("skills-state-machine", DeliveryRole.Projections);

    [Fact]
    public async Task ASuccessfulDeliveryIsAcknowledgedOnce()
    {
        var handler = new CountingHandler(DeliveryRole.Projections);
        var subscription = new FakeSubscription([ "first", "second" ]);

        await Run(subscription, handler);

        Assert.Equal([ "first", "second" ], handler.Bodies);
        Assert.Equal(2, subscription.Acknowledged);
        Assert.Equal(0, subscription.Redelivered);
    }

    [Fact]
    public async Task AFailedDeliveryIsRedeliveredAndNeverAcknowledged()
    {
        var handler = new CountingHandler(DeliveryRole.Projections, throws: true);
        var subscription = new FakeSubscription([ "first" ]);

        await Run(subscription, handler);

        Assert.Equal(0, subscription.Acknowledged);
        Assert.True(subscription.Redelivered >= 1);
    }

    [Fact]
    public async Task AQueueWithoutAHandlerForItsRoleFails()
    {
        var handler = new CountingHandler(DeliveryRole.Hooks);
        var subscription = new FakeSubscription([ "first" ]);

        await Run(subscription, handler);

        Assert.Equal(0, subscription.Acknowledged);
        Assert.Empty(handler.Bodies);
    }

    private static async Task Run(
        FakeSubscription subscription, CountingHandler handler)
    {
        var services = new ServiceCollection();
        services.AddScoped<IDeliveryHandler>(_ => handler);

        await using var provider = services.BuildServiceProvider();

        var worker = new QueueWorker(
            Queue,
            new FakeSubscriptionFactory(subscription),
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ConsumerSettings
            {
                ReceiveTimeout = TimeSpan.FromMilliseconds(10),
                RetryDelay = TimeSpan.FromMilliseconds(10),
                MaxRetryDelay = TimeSpan.FromMilliseconds(10)
            },
            NullLogger<QueueWorker>.Instance
        );

        await worker.StartAsync(CancellationToken.None);
        await subscription.Drained.WaitAsync(TimeSpan.FromSeconds(5));
        await worker.StopAsync(CancellationToken.None);
    }
}

internal sealed class CountingHandler(DeliveryRole role, bool throws = false)
    : IDeliveryHandler
{
    public List<string> Bodies { get; } = [ ];

    public DeliveryRole Role => role;

    public Task Handle(string body, CancellationToken cancellationToken)
    {
        if (throws)
            throw new InvalidOperationException("The projector failed.");

        Bodies.Add(body);
        return Task.CompletedTask;
    }
}

internal sealed class FakeSubscriptionFactory(IQueueSubscription _subscription)
    : IQueueSubscriptionFactory
{
    public Task<IQueueSubscription> Subscribe(
        string queueName, CancellationToken cancellationToken) =>
        Task.FromResult(_subscription);
}

internal sealed class FakeSubscription(List<string> bodies) : IQueueSubscription
{
    private readonly Queue<string> _pending = new(bodies);
    private readonly TaskCompletionSource _drained =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public int Acknowledged { get; private set; }
    public int Redelivered { get; private set; }

    /// <summary>Completes once the worker has taken every queued body.</summary>
    public Task Drained => _drained.Task;

    public async Task<string?> Receive(
        TimeSpan timeout, CancellationToken cancellationToken)
    {
        lock (_pending)
        {
            if (_pending.Count > 0)
                return _pending.Dequeue();
        }

        _drained.TrySetResult();

        // An empty queue waits like the real consumer does. Returning
        // synchronously would spin the worker loop without ever yielding.
        await Task.Delay(timeout, CancellationToken.None);
        return null;
    }

    public Task Acknowledge(CancellationToken cancellationToken)
    {
        Acknowledged++;
        return Task.CompletedTask;
    }

    public Task Redeliver(CancellationToken cancellationToken)
    {
        Redelivered++;
        return Task.CompletedTask;
    }

    public void Dispose() { }
}
