using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OutboxProcessingModule.Application;
using OutboxProcessingModule.Hosting;
using OutboxProcessingModule.Persistence;
using OutboxProcessingModule.Tests;

namespace OutboxProcessingModule.IntegrationTests;

[Collection(ArtemisCollection.Name)]
public sealed class OutboxDispatchStepTests : IDisposable
{
    private readonly OutboxDatabase _database = new();

    private readonly NmsConnectionManager _connections = new(
        ArtemisBroker.Options(), NullLogger<NmsConnectionManager>.Instance);

    public OutboxDispatchStepTests()
    {
        if (ArtemisBroker.IsReachable)
            ArtemisBroker.Drain(
                ArtemisBroker.ProjectionsQueue, ArtemisBroker.HooksQueue);
    }

    public void Dispose()
    {
        _connections.Dispose();
        _database.Dispose();
    }

    [BrokerFact]
    public async Task ACommittedCycleMarksEveryRowSentAndDeliversEveryMessage()
    {
        await Seed(rows: 2);

        var claimed = await CreateStep(ArtemisBroker.ProjectionsQueue)
            .Execute(CancellationToken.None);

        Assert.True(claimed);
        Assert.Equal(2, ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 2).Count);
        await AssertStored(MessageStatus.Sent, attempts: 1, hasError: false);
    }

    [BrokerFact]
    public async Task AFailedPublishLeavesNoRowSentAndReturnsThemToNew()
    {
        await Seed(rows: 2);

        var step = CreateStep(ArtemisBroker.UnprovisionedQueue);

        await Assert.ThrowsAsync<BatchPublicationException>(
            () => step.Execute(CancellationToken.None));

        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        await AssertStored(MessageStatus.New, attempts: 1, hasError: true);
    }

    [BrokerFact]
    public async Task ThePublisherRecoversTheConnectionAfterABrokerFailure()
    {
        await Seed(rows: 1);
        await Assert.ThrowsAsync<BatchPublicationException>(
            () => CreateStep(ArtemisBroker.UnprovisionedQueue)
                .Execute(CancellationToken.None));

        // The failed attempt reset the connection; the next cycle must build a
        // new one rather than reuse the broken session.
        var claimed = await CreateStep(ArtemisBroker.ProjectionsQueue)
            .Execute(CancellationToken.None);

        Assert.True(claimed);
        Assert.Single(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        await AssertStored(MessageStatus.Sent, attempts: 2, hasError: false);
    }

    [BrokerFact]
    public async Task ACycleWithNothingToClaimReportsNoWork()
    {
        var claimed = await CreateStep(ArtemisBroker.ProjectionsQueue)
            .Execute(CancellationToken.None);

        Assert.False(claimed);
    }

    [BrokerFact]
    public async Task RowsThatResolveToNoQueueAreSentWithoutTouchingTheBroker()
    {
        await Seed(rows: 2);

        var claimed = await CreateStep().Execute(CancellationToken.None);

        Assert.True(claimed);
        Assert.Empty(ArtemisBroker.ReceiveAll(ArtemisBroker.ProjectionsQueue, 1));
        await AssertStored(MessageStatus.Sent, attempts: 1, hasError: false);
    }

    private OutboxDispatchStep CreateStep(params string[] queues)
    {
        var context = _database.CreateContext();

        return new OutboxDispatchStep(
            new OutboxDispatchRepository(context),
            new FixedQueueResolver(queues.ToList()),
            new TransactionalOutboxPublisher(
                _connections, NullLogger<TransactionalOutboxPublisher>.Instance),
            ArtemisBroker.Options(),
            NullLogger<OutboxDispatchStep>.Instance
        );
    }

    private async Task AssertStored(
        MessageStatus status, int attempts, bool hasError)
    {
        using var context = _database.CreateContext();
        var stored = await context.SerializedPayloadMessage.ToListAsync();

        Assert.NotEmpty(stored);
        Assert.All(stored, row => Assert.Equal(status, row.Status));
        Assert.All(stored, row => Assert.Equal(attempts, row.ExecutionAttempts));
        Assert.All(
            stored,
            row => Assert.Equal(hasError, !string.IsNullOrEmpty(row.Error)));
    }

    private async Task Seed(int rows)
    {
        using var context = _database.CreateContext();

        for (var index = 0; index < rows; index++)
        {
            var row = TestData.Row(
                TestData.ExecutionInfo("skills-state-machine", "SkillCreatedV1"));
            row.Id = 0;
            context.SerializedPayloadMessage.Add(row);
        }

        await context.SaveChangesAsync();
    }
}

/// <summary>
/// Routing has its own tests; this sends every claimed row to the queues the
/// case under test needs, including one the broker does not provision.
/// </summary>
internal sealed class FixedQueueResolver(List<string> _queues) : IOutboxQueueResolver
{
    public List<string> ResolveQueues(SerializedPayloadMessage row) => _queues;

    public List<OutboxDispatch> Resolve(List<SerializedPayloadMessage> rows) =>
        rows.Select(row => new OutboxDispatch(row, _queues)).ToList();
}
