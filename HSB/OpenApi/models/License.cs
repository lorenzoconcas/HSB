using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;
public class License(string name, string url)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("url")]
    public string Url { get; set; } = url;
}