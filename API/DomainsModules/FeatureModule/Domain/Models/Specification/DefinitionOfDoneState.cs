using System.Text.Json.Serialization;

namespace FeatureModule.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DefinitionOfDoneState
{
    NotConfirmed,

    Confirmed,

    Failed,

    Postponed
}
