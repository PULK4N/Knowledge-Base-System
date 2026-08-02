using EventSourcing.Shared.Models;
using MemoryModule.Domain.Events;
using Shared.Interfaces;

namespace MemoryModule.Domain.Validators;

public sealed class SessionAggregateMappingMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        if (payload.EventData is not SessionAggregateMapAddedV1 eventData)
        {
            return EventValidationResult.FromPayload(
                payload,
                nameof(SessionAggregateMappingMustNotExistValidator),
                false,
                $"{nameof(SessionAggregateMappingMustNotExistValidator)} can only validate "
                    + $"{nameof(SessionAggregateMapAddedV1)} events."
            );
        }

        var state = (SessionAggregateMapStateData)stateData;
        var mappingExists = state.AggregateIdsBySession.ContainsKey(
            eventData.ThreadId
        );

        return EventValidationResult.FromPayload(
            payload,
            nameof(SessionAggregateMappingMustNotExistValidator),
            !mappingExists,
            mappingExists
                ? $"A session aggregate mapping for thread ID "
                    + $"'{eventData.ThreadId.Value}' already exists."
                : null
        );
    }
}
