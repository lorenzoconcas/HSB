using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class SecurityScheme(
    string type,
    string description,
    string name,
    string @in,
    string scheme,
    string bearerFormat,
    OAuthFlow flows,
    string openIdConnectUrl)
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = type;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("in")]
    public string In { get; set; } = @in;

    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = scheme;

    [JsonPropertyName("bearerFormat")]
    public string BearerFormat { get; set; } = bearerFormat;

    [JsonPropertyName("flows")]
    public OAuthFlow Flows { get; set; } = flows;

    [JsonPropertyName("openIdConnectUrl")]
    public string OpenIdConnectUrl { get; set; } = openIdConnectUrl;
}