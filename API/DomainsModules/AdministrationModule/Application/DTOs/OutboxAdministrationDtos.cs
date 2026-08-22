using AdministrationModule.Application.Persistence;

namespace AdministrationModule.Application.DTOs;

public sealed record OutboxPayloadDto(
    long Id,
    string State,
    int RetryCount,
    string? ErrorMessage,
    string StateMachineId,
    Guid AggregateId,
    uint OrderNumber,
    string EventName,
    DateTime Timestamp,
    string ExecutionInfoJson,
    string EventDataJson
)
{
    public static OutboxPayloadDto FromEntry(
        OutboxPayloadEntry entry
    ) =>
        new(
            entry.Id,
            entry.State,
            entry.RetryCount,
            entry.ErrorMessage,
            entry.StateMachineId,
            entry.AggregateId,
            entry.OrderNumber,
            entry.EventName,
            entry.Timestamp,
            entry.ExecutionInfoJson,
            entry.EventDataJson
        );
}
