using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Tag(string name, string description, ExternalDocs externalDocs)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("externalDocs")]
    public ExternalDocs ExternalDocs { get; set; } = externalDocs;
}