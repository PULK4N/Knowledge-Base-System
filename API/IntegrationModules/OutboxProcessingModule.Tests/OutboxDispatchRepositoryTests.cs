using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using OutboxProcessingModule.Persistence;

namespace OutboxProcessingModule.Tests;

public sealed class OutboxDispatchRepositoryTests : IDisposable
{
    private readonly OutboxDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task ClaimTakesTheOldestNewRowsUpToTheLimit()
    {
        await Seed(MessageStatus.New, MessageStatus.New, MessageStatus.New);

        using var context = _database.CreateContext();
        var rows = await new OutboxDispatchRepository(context)
            .Claim(limit: 2, CancellationToken.None);

        Assert.Equal([ 1L, 2L ], rows.Select(row => row.Id));
        Assert.All(rows, row => Assert.Equal(MessageStatus.Reading, row.Status));
        Assert.All(rows, row => Assert.Equal(1, row.ExecutionAttempts));
        Assert.Equal(MessageStatus.New, await StatusOf(3));
    }

    [Theory]
    [InlineData(MessageStatus.Reading)]
    [InlineData(MessageStatus.Error)]
    [InlineData(MessageStatus.Sent)]
    public async Task ClaimIgnoresRowsThatAreNotNew(MessageStatus status)
    {
        await Seed(status);

        using var context = _database.CreateContext();
        var rows = await new OutboxDispatchRepository(context)
            .Claim(limit: 10, CancellationToken.None);

        Assert.Empty(rows);
    }

    [Fact(Skip = "Covered by the step 8 integration test against a real PostgreSQL "
        + "database. Sqlite has no store-generated row version, so this version "
        + "proves the harness as much as the repository.")]
    public async Task TheSecondClaimerLosesTheRaceAndBurnsNoAttempt()
    {
        await Seed(MessageStatus.New);

        using var winnerContext = _database.CreateContext();
        using var loserContext = _database.CreateContext();
        var winner = new OutboxDispatchRepository(winnerContext);
        var loser = new OutboxDispatchRepository(loserContext);

        // The winner claims between the loser reading the row and writing it,
        // which is the only window where two publishers can collide.
        loserContext.SavingChanges += (_, _) =>
            winner.Claim(limit: 10, CancellationToken.None).GetAwaiter().GetResult();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => loser.Claim(limit: 10, CancellationToken.None));

        using var reader = _database.CreateContext();
        var row = await reader.SerializedPayloadMessage.SingleAsync();
        Assert.Equal(MessageStatus.Reading, row.Status);
        Assert.Equal(1, row.ExecutionAttempts);
    }

    [Fact]
    public async Task CompleteMarksEveryClaimedRowSentAndClearsTheError()
    {
        await Seed(MessageStatus.New, MessageStatus.New);

        using var context = _database.CreateContext();
        var repository = new OutboxDispatchRepository(context);
        var rows = await repository.Claim(limit: 10, CancellationToken.None);

        await repository.Complete(rows, CancellationToken.None);

        using var reader = _database.CreateContext();
        var stored = await reader.SerializedPayloadMessage.ToListAsync();
        Assert.All(stored, row => Assert.Equal(MessageStatus.Sent, row.Status));
        Assert.All(stored, row => Assert.Null(row.Error));
    }

    [Fact]
    public async Task FailReturnsTheRowToNewUntilAttemptsRunOut()
    {
        await Seed(MessageStatus.New);

        var observed = new List<(MessageStatus Status, int Attempts)>();

        for (var attempt = 0; attempt < SerializedPayloadMessage.MaxExecutionAttempts; attempt++)
        {
            using var context = _database.CreateContext();
            var repository = new OutboxDispatchRepository(context);

            var rows = await repository.Claim(limit: 10, CancellationToken.None);
            await repository.Fail(rows, "broker unreachable", CancellationToken.None);

            using var reader = _database.CreateContext();
            var row = await reader.SerializedPayloadMessage.SingleAsync();
            observed.Add((row.Status, row.ExecutionAttempts));
            Assert.Equal("broker unreachable", row.Error);
        }

        Assert.Equal(
            [
                (MessageStatus.New, 1),
                (MessageStatus.New, 2),
                (MessageStatus.Error, 3)
            ],
            observed
        );
    }

    private async Task<MessageStatus> StatusOf(long id)
    {
        using var context = _database.CreateContext();
        return (await context.SerializedPayloadMessage.SingleAsync(row => row.Id == id))
            .Status;
    }

    private async Task Seed(params MessageStatus[] statuses)
    {
        using var context = _database.CreateContext();

        foreach (var status in statuses)
        {
            var info = TestData.ExecutionInfo("skills-state-machine", "SkillCreatedV1");
            var row = TestData.Row(info);
            row.Id = 0;
            row.Status = status;
            context.SerializedPayloadMessage.Add(row);
        }

        await context.SaveChangesAsync();
    }
}
