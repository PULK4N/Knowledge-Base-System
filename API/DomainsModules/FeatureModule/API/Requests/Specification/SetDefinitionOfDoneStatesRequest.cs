using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record SetDefinitionOfDoneStatesRequest
{
    public required List<DefinitionOfDoneStateInput> States { get; init; }
}
