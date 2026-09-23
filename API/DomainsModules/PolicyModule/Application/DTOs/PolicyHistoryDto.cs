using PolicyModule.Domain.Models;

namespace PolicyModule.Application.DTOs;

public sealed record PolicyHistoryDto(
    Guid PolicyId,
    string Title,
    string Description,
    IReadOnlyCollection<PolicyMemoryHistoryDto> MemoryHistory
)
{
    public static PolicyHistoryDto FromModel(
        Policy policy,
        IEnumerable<MemoryHistoryRecord> history
    ) =>
        new(
            policy.PolicyId.Value,
            policy.Title,
            policy.Description,
            history
                .Where(record => record.PolicyId == policy.PolicyId)
                .Select(PolicyMemoryHistoryDto.FromModel)
                .ToList()
        );
}

public sealed record PolicyMemoryHistoryDto(
    string EventName,
    DateTime Timestamp,
    Guid MemoryId,
    bool IsUserOriginated
)
{
    public static PolicyMemoryHistoryDto FromModel(MemoryHistoryRecord record) =>
        new(
            record.EventName,
            record.Timestamp,
            record.AggregateId.Value,
            record.AggregateId.Value == SharedModule.Constants.MemoryAggregateIds.User
        );
}
