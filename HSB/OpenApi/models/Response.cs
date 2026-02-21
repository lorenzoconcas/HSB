using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Response
{
    public Response(string description)
    {
        this.Description = description;
    }
    
    public Response(
        string description,
        Dictionary<string, Header> headers,
        Dictionary<string, MediaType> content,
        Dictionary<string, Link> links,
        string @ref)
    {
        this.Description = description;
        this.Headers = headers;
        this.Content = content;
        this.Links = links;
        this.Ref = @ref;
    }
    
    [JsonPropertyName("description")]
    public string Description { get; set; } 

    [JsonPropertyName("headers")]
    public Dictionary<string, Header>? Headers { get; set; } 

    [JsonPropertyName("content")]
    public Dictionary<string, MediaType>? Content { get; set; } 

    [JsonPropertyName("links")]
    public Dictionary<string, Link>? Links { get; set; } 

    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
}