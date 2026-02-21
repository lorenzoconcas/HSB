using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Example(string summary, string description, object value, string externalValue)
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = summary;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("value")]
    public object Value { get; set; } = value;

    [JsonPropertyName("externalValue")]
    public string ExternalValue { get; set; } = externalValue;
}