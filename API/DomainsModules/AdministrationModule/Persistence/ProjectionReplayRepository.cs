using AdministrationModule.Application.Persistence;
using EventSourcing.Persistence;
using EventSourcing.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AdministrationModule.Persistence;

public sealed class ProjectionReplayRepository(
    EventSourcingDbContext dbContext
) : IProjectionReplayRepository
{
    public async Task<List<EventPayload>> GetLastEvents(
        string stateMachineId
    )
    {
        var lastOrderNumbers = dbContext.SerializedEventPayload
            .AsNoTracking()
            .Where(
                payload =>
                    payload.StateMachineId == stateMachineId
            )
            .GroupBy(payload => payload.AggregateId)
            .Select(
                aggregate =>
                    new
                    {
                        AggregateId = aggregate.Key,
                        OrderNumber = aggregate.Max(
                            payload => payload.OrderNumber
                        )
                    }
            );
        var lastEvents = await dbContext.SerializedEventPayload
            .AsNoTracking()
            .Join(
                lastOrderNumbers,
                payload =>
                    new
                    {
                        payload.AggregateId,
                        payload.OrderNumber
                    },
                last =>
                    new
                    {
                        last.AggregateId,
                        last.OrderNumber
                    },
                (payload, _) => payload
            )
            .OrderBy(payload => payload.AggregateId)
            .ToListAsync();

        return lastEvents
            .Select(payload => payload.Deserialize())
            .ToList();
    }
}
