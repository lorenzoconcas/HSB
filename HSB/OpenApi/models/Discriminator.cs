using System.Text.Json.Serialization;
namespace HSB.OpenApi.models;

public class Discriminator(string propertyName, Dictionary<string, string> mapping)
{
    [JsonPropertyName("propertyName")]
    public string PropertyName { get; set; } = propertyName;

    [JsonPropertyName("mapping")]
    public Dictionary<string, string> Mapping { get; set; } = mapping;
}