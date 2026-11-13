using System.Text.Json.Serialization;

namespace FeatureModule.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RequirementType
{
    Business,

    Functional,

    NonFunctional,

    Technical
}
