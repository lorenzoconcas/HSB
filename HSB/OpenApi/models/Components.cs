using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Components
{
    public Components(
        Dictionary<string, Schema> schemas,
        Dictionary<string, Response>? responses = null,
        Dictionary<string, Parameter>? parameters = null,
        Dictionary<string, Example>? examples = null,
        Dictionary<string, RequestBody>? requestBodies = null,
        Dictionary<string, Header>? headers = null,
        Dictionary<string, SecurityScheme>? securitySchemes = null,
        Dictionary<string, Link>? links = null,
        Dictionary<string, Dictionary<string, PathItem>>? callbacks = null)
    {
        Schemas = schemas;
        Responses = responses ?? [];
        Parameters = parameters ?? [];
        Examples = examples ?? [];
        RequestBodies = requestBodies ?? [];
        Headers = headers ?? [];
        SecuritySchemes = securitySchemes ?? [];
        Links = links ?? [];
        Callbacks = callbacks ?? [];
    }

    [JsonPropertyName("schemas")] public Dictionary<string, Schema> Schemas { get; set; }

    [JsonPropertyName("responses")] public Dictionary<string, Response> Responses { get; set; }

    [JsonPropertyName("parameters")] public Dictionary<string, Parameter> Parameters { get; set; }

    [JsonPropertyName("examples")] public Dictionary<string, Example> Examples { get; set; }

    [JsonPropertyName("requestBodies")] public Dictionary<string, RequestBody> RequestBodies { get; set; }

    [JsonPropertyName("headers")] public Dictionary<string, Header> Headers { get; set; }

    [JsonPropertyName("securitySchemes")] public Dictionary<string, SecurityScheme> SecuritySchemes { get; set; }

    [JsonPropertyName("links")] public Dictionary<string, Link> Links { get; set; }

    [JsonPropertyName("callbacks")] public Dictionary<string, Dictionary<string, PathItem>> Callbacks { get; set; }
}