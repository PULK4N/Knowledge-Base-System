using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillProjectMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var projectId = payload.EventData switch
        {
            SkillProjectRelationAddedV1 eventData => eventData.ProjectId,
            _ => throw new InvalidCastException()
        };
        var exists = ((SkillStateData)stateData)
            .RelatedProjectIds
            .Contains(projectId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillProjectMustNotExistValidator),
            !exists,
            exists
                ? $"Project '{projectId.Value}' is already linked to the skill."
                : null
        );
    }
}
