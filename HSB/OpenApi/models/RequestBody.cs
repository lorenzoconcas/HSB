using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class RequestBody(string description, Dictionary<string, MediaType> content, bool? required, string @ref)
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("content")]
    public Dictionary<string, MediaType> Content { get; set; } = content;

    [JsonPropertyName("required")]
    public bool? Required { get; set; } = required;

    [JsonPropertyName("$ref")]
    public string Ref { get; set; } = @ref;
}