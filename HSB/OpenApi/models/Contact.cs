using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Contact
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
