using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace SkillsModule.Domain.Validators;

public sealed class SkillMustBeActiveValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (SkillStateData)stateData;
        var exists = !string.IsNullOrWhiteSpace(state.Name);
        var failureReason = !exists
            ? "The skill does not exist."
            : state.IsDeleted
                ? "The skill is deleted."
                : null;

        return EventValidationResult.FromPayload(
            payload,
            nameof(SkillMustBeActiveValidator),
            failureReason is null,
            failureReason
        );
    }
}
