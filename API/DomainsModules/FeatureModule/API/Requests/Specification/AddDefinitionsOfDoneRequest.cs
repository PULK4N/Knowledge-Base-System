using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record AddDefinitionsOfDoneRequest
{
    public required List<DefinitionOfDoneInput> DefinitionsOfDone { get; init; }
}
