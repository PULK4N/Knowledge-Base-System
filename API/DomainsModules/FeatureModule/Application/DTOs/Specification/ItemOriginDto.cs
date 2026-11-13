using FeatureModule.Domain.Models;

namespace FeatureModule.Application.DTOs;

/// <summary>The plan item a feature copy came from, for the "origin" link.</summary>
public sealed record ItemOriginDto(Guid PlanId, Guid PlanItemId)
{
    public static ItemOriginDto? FromModel(ItemOrigin? origin) =>
        origin is null ? null : new(origin.PlanId.Value, origin.PlanItemId);
}
