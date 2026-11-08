using EventSourcing.Shared.Models;
using ProjectModule.Domain.Events;
using Shared.Interfaces;

namespace ProjectModule.Domain.Validators;

public sealed class ProjectSkillMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(
        object stateData,
        EventPayload payload
    )
    {
        var skillId = payload.EventData switch
        {
            ProjectSkillRelationRemovedV1 data => data.SkillId,
            _ => throw new InvalidCastException()
        };
        var exists = ((ProjectStateData)stateData)
            .RelatedSkillIds
            .Contains(skillId);

        return EventValidationResult.FromPayload(
            payload,
            nameof(ProjectSkillMustExistValidator),
            exists,
            exists
                ? null
                : $"Skill '{skillId.Value}' is not linked to the project."
        );
    }
}
