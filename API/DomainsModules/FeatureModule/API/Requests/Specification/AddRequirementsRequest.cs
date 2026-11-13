using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record AddRequirementsRequest
{
    public required List<RequirementInput> Requirements { get; init; }
}
