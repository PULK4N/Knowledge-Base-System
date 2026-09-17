using System.Text.Json.Serialization;

namespace MemoryModule.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter<MemoryEntityType>))]
public enum MemoryEntityType
{
    Feature,
    Skill,
    FeaturePlan,
    FeatureResearchDiscovery,
    FeatureRecord,
    FeatureReviewNote,
    SkillAttachment,
    Project,
    Policy
}

public readonly record struct MemoryEntityId(Guid Value);

public sealed record MemoryRelatedEntity(MemoryEntityType Type, MemoryEntityId Id);
