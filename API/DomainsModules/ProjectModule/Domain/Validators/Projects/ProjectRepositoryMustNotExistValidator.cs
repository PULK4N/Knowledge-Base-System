using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectRepositoryMustNotExistValidator
    : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var repositoryPath = payload.EventData switch
        {
            RepositoryAddedToProjectV1 data => data.RepositoryPath,
            RepositoryAddedToProjectV2 data => data.RepositoryPath,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData)
            .RepositoryPaths
            .Contains(repositoryPath, StringComparer.Ordinal);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectRepositoryMustNotExistValidator),
            !exists,
            exists
                ? $"Repository path '{repositoryPath}' is already assigned to the project."
                : null
        );
    }
}
