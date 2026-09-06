using EventSourcing.Persistence;
using EventSourcing.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using OutboxProcessingModule.Application;

namespace OutboxProcessingModule.Persistence;

public sealed class OutboxDispatchRepository(
    EventSourcingDbContext _dbContext
) : IOutboxDispatchRepository
{
    /// <summary>
    /// Status alone selects the rows: an exhausted row is already Error and a
    /// claimed row is already Reading, so neither is New. The row version turns
    /// a lost race into a failed save rather than a second claim, so no lease
    /// column and no row lock is needed.
    /// </summary>
    public async Task<List<SerializedPayloadMessage>> Claim(
        int limit, CancellationToken cancellationToken)
    {
        var rows = await _dbContext
            .SerializedPayloadMessage
            .Where(row => row.Status == MessageStatus.New)
            .OrderBy(row => row.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return rows;

        foreach (var row in rows)
            row.StartReading();

        // Throws DbUpdateConcurrencyException when another publisher already
        // moved any of these rows off New. The increment never commits then,
        // so a conflict burns no attempt.
        await _dbContext.SaveChangesAsync(cancellationToken);

        return rows;
    }

    public async Task Complete(
        List<SerializedPayloadMessage> rows, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
            row.Complete();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Fail(
        List<SerializedPayloadMessage> rows, string error,
        CancellationToken cancellationToken)
    {
        foreach (var row in rows)
            row.Fail(error);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
