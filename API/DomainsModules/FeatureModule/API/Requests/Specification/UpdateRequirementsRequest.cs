using FeatureModule.Application.Commands;

namespace FeatureModule.API.Requests;

public sealed record UpdateRequirementsRequest
{
    public required List<RequirementChangeInput> Requirements { get; init; }
}
