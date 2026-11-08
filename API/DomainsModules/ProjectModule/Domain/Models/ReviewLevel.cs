using System.Text.Json.Serialization;

namespace ProjectModule.Domain.Models;

/// <summary>
/// How demanding a skill's review is. Stored by name, so new levels can be
/// added without a new event version.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewLevel
{
    Easy,
    Medium,
    Heavy
}
