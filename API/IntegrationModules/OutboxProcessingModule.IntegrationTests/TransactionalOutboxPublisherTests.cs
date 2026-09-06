using System.Text.Json;
using EventSourcing.Persistence.Models;
using Microsoft.Extensions.Logging.Abstractions;
using OutboxProcessingModule.Application;
using OutboxProcessingModule.Hosting;
using OutboxProcessingModule.Tests;

namespace OutboxProcessingModule.IntegrationTests;

[Collection(ArtemisCollection.Name)]
public sealed class TransactionalOutboxPublisherTests : IDisposable
{
    private readonly NmsConnectionManager _connections = new(
        ArtemisBroker.Options(), NullLogger<NmsConnectionManager>.Instance);

    private readonly TransactionalOutboxPublisher _publisher;

    public TransactionalOutboxPublisherTests()
    {
        _publisher = new TransactionalOutboxPublisher(
            _connections, NullLogger<TransactionalOutboxPublisher>.Instance);

        if (ArtemisBroker.IsReachable)
            ArtemisBroker.Drain(
                ArtemisBroker.ProjectionsQueue, ArtemisBroker.HooksQueue);
    }

    public void Dispose() => _connections.Dispose();

    [BrokerFact]
    public async Task ARowBoundForTwoQueuesReachesBoth()
    {
        var row = Row();

        await _publisher.Publish(
            [
                new OutboxDispatch(
                    row, [ ArtemisBroker.ProjectionsQueue, ArtemisBroker.HooksQueue ])
            ],
            CancellationToken.None);

        var projections = ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1);
        var hooks = ArtemisBroker.ReceiveAll(ArtemisBroker.HooksQueue, 1);

        Assert.Equal(row.Id, IdOf(Assert.Single(projections)));
        Assert.Equal(row.Id, IdOf(Assert.Single(hooks)));
    }

    [BrokerFact]
    public async Task AFailedSendRollsBackEverySendThatCameBeforeIt()
    {
        var delivered = Row();
        var undeliverable = Row();

        await Assert.ThrowsAsync<BatchPublicationException>(
            () => _publisher.Publish(
                [
                    new OutboxDispatch(
                        delivered,
                        [ ArtemisBroker.ProjectionsQueue, ArtemisBroker.HooksQueue ]),
                    new OutboxDispatch(
                        undeliverable, [ ArtemisBroker.UnprovisionedQueue ])
                ],
                CancellationToken.None));

        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.HooksQueue, 1));
    }

    [BrokerFact]
    public async Task NothingIsDeliveredWhenTheAttemptIsCancelledBeforeTheCommit()
    {
        using var cancellation = new CancellationTokenSource();

        var task = _publisher.Publish(
            [
                new OutboxDispatch(Row(), [ ArtemisBroker.ProjectionsQueue ]),
                new OutboxDispatch(Row(), [ ArtemisBroker.ProjectionsQueue ])
            ],
            cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAsync<BatchPublicationException>(() => task);
        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
    }

    [BrokerFact]
    public async Task ARowThatResolvesToNoQueueSendsNothing()
    {
        await _publisher.Publish(
            [ new OutboxDispatch(Row(), [ ]) ], CancellationToken.None);

        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.HooksQueue, 1));
    }

    [BrokerFact]
    public async Task ThePublishedBodyIsTheStoredRowAndDeserializesBack()
    {
        var row = Row();

        await _publisher.Publish(
            [ new OutboxDispatch(row, [ ArtemisBroker.ProjectionsQueue ]) ],
            CancellationToken.None);

        var body = Assert.Single(
            ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        var wireRow = JsonSerializer.Deserialize<SerializedPayloadMessage>(body)
            ?? throw new InvalidOperationException("The message body was not a row.");

        Assert.Equal(row.Id, wireRow.Id);
        Assert.Equal(row.AggregateId, wireRow.AggregateId);
        Assert.Equal(
            row.SerializedEventExecutionInfo, wireRow.SerializedEventExecutionInfo);
        Assert.Equal(row.SerializedEventData, wireRow.SerializedEventData);
    }

    private static long _nextId = 1;

    private static SerializedPayloadMessage Row()
    {
        var row = TestData.Row(
            TestData.ExecutionInfo("skills-state-machine", "SkillCreatedV1"));
        row.Id = Interlocked.Increment(ref _nextId);
        return row;
    }

    private static long IdOf(string body) =>
        JsonSerializer.Deserialize<SerializedPayloadMessage>(body)!.Id;
}
