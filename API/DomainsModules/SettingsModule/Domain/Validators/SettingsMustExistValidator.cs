using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace SettingsModule.Domain.Validators;

public sealed class SettingsMustExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (SettingsStateData)stateData;
        var valid = state.IsInitialized;
        return EventValidationResult.FromPayload(payload, nameof(SettingsMustExistValidator), valid,
            valid ? null : "Settings have not been initialized.");
    }
}
