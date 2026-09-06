using EventSourcing.Persistence.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OutboxProcessingModule.Application;
using OutboxProcessingModule.Hosting;
using OutboxProcessingModule.Tests;

namespace OutboxProcessingModule.IntegrationTests;

[Collection(ArtemisCollection.Name)]
public sealed class NmsQueueSubscriptionTests : IDisposable
{
    private readonly NmsConnectionManager _connections = new(
        ArtemisBroker.Options(), NullLogger<NmsConnectionManager>.Instance);

    private readonly NmsQueueSubscriptionFactory _subscriptions =
        new(ArtemisBroker.Options());

    public NmsQueueSubscriptionTests()
    {
        if (ArtemisBroker.IsReachable)
            ArtemisBroker.Drain(ArtemisBroker.ProjectionsQueue);
    }

    public void Dispose() => _connections.Dispose();

    [BrokerFact]
    public async Task AnAcknowledgedMessageIsGoneFromTheQueue()
    {
        var row = await Publish();

        using var subscription = await _subscriptions.Subscribe(
            ArtemisBroker.ProjectionsQueue, CancellationToken.None);

        Assert.Equal(row.Id, IdOf(await Receive(subscription)));
        await subscription.Acknowledge(CancellationToken.None);

        Assert.Null(await Receive(subscription));
    }

    [BrokerFact]
    public async Task ARolledBackMessageComesBack()
    {
        var row = await Publish();

        using var subscription = await _subscriptions.Subscribe(
            ArtemisBroker.ProjectionsQueue, CancellationToken.None);

        Assert.Equal(row.Id, IdOf(await Receive(subscription)));
        await subscription.Redeliver(CancellationToken.None);

        // The broker's redelivery-delay is 5 seconds for this address.
        Assert.Equal(row.Id, IdOf(await Receive(subscription, seconds: 15)));
        await subscription.Acknowledge(CancellationToken.None);
    }

    [BrokerFact]
    public async Task AnEmptyQueueReturnsNothingRatherThanBlockingForever()
    {
        using var subscription = await _subscriptions.Subscribe(
            ArtemisBroker.ProjectionsQueue, CancellationToken.None);

        Assert.Null(await Receive(subscription));
    }

    private static Task<string?> Receive(
        SharedModule.DistributedMessaging.Consuming.IQueueSubscription subscription,
        int seconds = 3) =>
        subscription.Receive(TimeSpan.FromSeconds(seconds), CancellationToken.None);

    private async Task<SerializedPayloadMessage> Publish()
    {
        var row = TestData.Row(
            TestData.ExecutionInfo("skills-state-machine", "SkillCreatedV1"));
        row.Id = Random.Shared.NextInt64(1, long.MaxValue);

        var publisher = new TransactionalOutboxPublisher(
            _connections, NullLogger<TransactionalOutboxPublisher>.Instance);

        await publisher.Publish(
            [ new OutboxDispatch(row, [ ArtemisBroker.ProjectionsQueue ]) ],
            CancellationToken.None);

        return row;
    }

    private static long IdOf(string? body) =>
        System.Text.Json.JsonSerializer
            .Deserialize<SerializedPayloadMessage>(body!)!.Id;
}
