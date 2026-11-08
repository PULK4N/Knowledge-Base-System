using EventSourcing.Shared.Models;
using Shared.Interfaces;

namespace SettingsModule.Domain.Validators;

public sealed class SettingsMustNotExistValidator : IPreEventValidator
{
    public EventValidationResult Validate(object stateData, EventPayload payload)
    {
        var state = (SettingsStateData)stateData;
        var valid = !state.IsInitialized;
        return EventValidationResult.FromPayload(payload, nameof(SettingsMustNotExistValidator), valid,
            valid ? null : "Settings are already initialized.");
    }
}
