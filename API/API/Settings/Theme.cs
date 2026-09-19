using System.Text.Json.Serialization;

namespace Api.Settings;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Theme
{
    Light,
    Dark
}
