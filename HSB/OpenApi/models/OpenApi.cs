using HSB.OpenApi.models;

namespace HSB.OpenApi.models;

using System.Collections.Generic;
using System.Text.Json.Serialization;


public class OpenApi
{
    public OpenApi(string openapi)
    {
        Openapi = openapi;
    }
    
    
    public OpenApi(string openapi,
        Info info,
        List<Server> servers,
        Dictionary<string, PathItem> paths,
        Components components,
        List<Dictionary<string, List<string>>> security,
        List<Tag> tags,
        ExternalDocs externalDocs)
    {
        Openapi = openapi;
        Info = info;
        Servers = servers;
        Paths = paths;
        Components = components;
        Security = security;
        Tags = tags;
        ExternalDocs = externalDocs;
    }

    [JsonPropertyName("openapi")]
    public string Openapi { get; set; }

    [JsonPropertyName("info")]
    public Info? Info { get; set; }

    [JsonPropertyName("servers")]
    public List<Server>? Servers { get; set; }

    [JsonPropertyName("paths")]
    public Dictionary<string, PathItem>? Paths { get; set; }

    [JsonPropertyName("components")]
    public Components? Components { get; set; }

    [JsonPropertyName("security")]
    public List<Dictionary<string, List<string>>>? Security { get; set; }

    [JsonPropertyName("tags")]
    public List<Tag>? Tags { get; set; }

    [JsonPropertyName("externalDocs")]
    public ExternalDocs? ExternalDocs { get; set; }
}