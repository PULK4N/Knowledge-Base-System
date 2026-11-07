using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class RepositoryMappingMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData =
            (RepositoryToProjectMapAddedV1)payload.EventData;
        var exists = ((RepositoryToProjectMapStateData)stateData)
            .RepositoryToProjectMap
            .ContainsKey(eventData.RepositoryPath);

        return EventValidationResult.FromPayload(
            payload,
            nameof(RepositoryMappingMustNotExistValidator),
            !exists,
            exists
                ? $"Repository path '{eventData.RepositoryPath}' is already mapped to a project."
                : null
        );
    }
}
