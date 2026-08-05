using EventSourcing.Shared.Models;
using PolicyModule.Domain.Events;
using Shared.Interfaces;

namespace PolicyModule.Domain.Validators;

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
