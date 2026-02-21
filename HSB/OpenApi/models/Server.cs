using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Server
{
    public Server(){}

    public Server(string? url, string? description, Dictionary<string, ServerVariable>? variables)
    {
        this.Url = url;
        this.Description = description;
        this.Variables = variables;
    }
    
    [JsonPropertyName("url")]
    public string? Url { get; set; } 

    [JsonPropertyName("description")]
    public string? Description { get; set; } 

    [JsonPropertyName("variables")]
    public Dictionary<string, ServerVariable>? Variables { get; set; } 
}