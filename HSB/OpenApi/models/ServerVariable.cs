using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class ServerVariable(List<string> @enum, string @default, string description)
{
    [JsonPropertyName("enum")]
    public List<string> Enum { get; set; } = @enum;

    [JsonPropertyName("default")]
    public string Default { get; set; } = @default;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;
}