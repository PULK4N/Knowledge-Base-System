using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class RepositoryMappingMustExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var eventData =
            (RepositoryToProjectMapRemovedV1)payload.EventData;
        var mappings = ((RepositoryToProjectMapStateData)stateData)
            .RepositoryToProjectMap;
        var exists = mappings.TryGetValue(
            eventData.RepositoryPath,
            out var mappedProjectId
        ) && mappedProjectId == eventData.ProjectAggregateId;

        return EventValidationResult.FromPayload(
            payload,
            nameof(RepositoryMappingMustExistValidator),
            exists,
            exists
                ? null
                : $"Repository path '{eventData.RepositoryPath}' is not mapped to project '{eventData.ProjectAggregateId.Value}'."
        );
    }
}
