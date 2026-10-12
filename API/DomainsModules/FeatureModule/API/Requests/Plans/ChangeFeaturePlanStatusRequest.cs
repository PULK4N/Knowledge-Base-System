using FeatureModule.Domain.Models;

namespace FeatureModule.API.Requests;

public sealed record ChangeFeaturePlanStatusRequest
{
    public required FeaturePlanStatus Status { get; init; }
}
