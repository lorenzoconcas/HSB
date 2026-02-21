using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class ExternalDocs(string description, string url)
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("url")]
    public string Url { get; set; } = url;
}