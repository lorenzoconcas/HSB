using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Encoding(
    string contentType,
    Dictionary<string, Header> headers,
    string style,
    bool? explode,
    bool? allowReserved)
{
    [JsonPropertyName("contentType")] 
    public string ContentType { get; set; } = contentType;

    [JsonPropertyName("headers")] public Dictionary<string, Header> Headers { get; set; } = headers;

    [JsonPropertyName("style")]
    public string Style { get; set; } = style;

    [JsonPropertyName("explode")]
    public bool? Explode { get; set; } = explode;

    [JsonPropertyName("allowReserved")]
    public bool? AllowReserved { get; set; } = allowReserved;
}