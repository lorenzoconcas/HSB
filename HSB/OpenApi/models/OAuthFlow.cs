using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class OAuthFlow(string authorizationUrl, string tokenUrl, string refreshUrl, Dictionary<string, string> scopes)
{
    [JsonPropertyName("authorizationUrl")]
    public string AuthorizationUrl { get; set; } = authorizationUrl;

    [JsonPropertyName("tokenUrl")]
    public string TokenUrl { get; set; } = tokenUrl;

    [JsonPropertyName("refreshUrl")]
    public string RefreshUrl { get; set; } = refreshUrl;

    [JsonPropertyName("scopes")]
    public Dictionary<string, string> Scopes { get; set; } = scopes;
}