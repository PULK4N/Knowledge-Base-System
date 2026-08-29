using System.ComponentModel.DataAnnotations;
using ActionModule.Shared.Models;
using AdministrationModule.Application.Persistence;

namespace AdministrationModule.API.Requests;

public sealed record SearchOutboxPayloadsRequest : PagedSearchRequest
{
    public bool OnlyIncomplete { get; init; }

    [StringLength(EntityQueryLimits.MaximumSearchLength)]
    public string? State { get; init; }

    public Guid? AggregateId { get; init; }

    [EnumDataType(typeof(OutboxPayloadSortField))]
    public OutboxPayloadSortField SortBy { get; init; } =
        OutboxPayloadSortField.Id;
}
