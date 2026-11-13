using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record UpdateDefinitionsOfDoneRequest
{
    public required List<DefinitionOfDoneChangeInput> DefinitionsOfDone { get; init; }
}
