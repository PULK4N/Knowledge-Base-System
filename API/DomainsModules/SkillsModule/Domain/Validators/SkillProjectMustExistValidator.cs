using EventSourcing.Shared.Models;
using Shared.Interfaces;
using SkillsModule.Domain.Events;

namespace SkillsModule.Domain.Validators;

public sealed class SkillProjectMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var projectId = payload.EventData switch
        {
            SkillProjectRelationRemovedV1 eventData => eventData.ProjectId,
            _ => throw new InvalidCastException()
        };
        var exists = ((SkillStateData)stateData)
            .RelatedProjectIds
            .Contains(projectId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillProjectMustExistValidator),
            exists,
            exists
                ? null
                : $"Project '{projectId.Value}' is not linked to the skill."
        );
    }
}
