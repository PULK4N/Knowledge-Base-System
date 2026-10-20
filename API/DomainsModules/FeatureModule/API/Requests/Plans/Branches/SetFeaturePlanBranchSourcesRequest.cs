using FeatureModule.Application.DTOs;

namespace FeatureModule.API.Requests;

public sealed record SetFeaturePlanBranchSourcesRequest
{
    public required string BranchName { get; init; }

    /// <summary>
    /// An empty list goes back to the default source.
    /// </summary>
    public List<FeaturePlanBranchSourceDto> Sources { get; init; } = [];
}
