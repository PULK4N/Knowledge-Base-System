using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectSkillMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var skillId = payload.EventData switch
        {
            ProjectSkillRelationAddedV1 data => data.SkillId,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData)
            .RelatedSkillIds
            .Contains(skillId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectSkillMustNotExistValidator),
            !exists,
            exists
                ? $"Skill '{skillId.Value}' is already linked to the project."
                : null
        );
    }
}
