using System.Text.Json.Serialization;

namespace ProjectModule.Shared;

/// <summary>
/// How demanding a review is. Stored by name, so new levels can be
/// added without a new event version.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReviewLevel
{
    Easy,
    Medium,
    Heavy
}
