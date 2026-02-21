using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Operation {
    
    
    public Operation(
        string summary, 
        string description, 
        string operationId, 
        List<Parameter> parameters, 
        Dictionary<string, Response> responses)
    {
        this.Summary = summary;
        this.Description = description;
        this.OperationId = operationId;
        this.Parameters = parameters;
        this.Responses = responses;
    }


    public Operation(List<string> tags,
        string summary,
        string description,
        ExternalDocs externalDocs,
        string operationId,
        List<Parameter> parameters,
        RequestBody requestBody,
        Dictionary<string, Response> responses,
        Dictionary<string, Dictionary<string, PathItem>> callbacks,
        bool? deprecated,
        List<Dictionary<string, List<string>>> security,
        List<Server> servers)
    {
        this.Summary = summary;
        this.Description = description;
        this.OperationId = operationId;
        this.Parameters = parameters;
        this.Responses = responses;
        this.Callbacks = callbacks;
        this.Deprecated = deprecated;
        this.Security = security;
        this.Servers = servers;
    }
    
    
    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } 
    [JsonPropertyName("description")]
    public string Description { get; set; } 

    [JsonPropertyName("externalDocs")]
    public ExternalDocs? ExternalDocs { get; set; } 

    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } 

    [JsonPropertyName("parameters")]
    public List<Parameter> Parameters { get; set; }

    [JsonPropertyName("requestBody")]
    public RequestBody? RequestBody { get; set; }

    [JsonPropertyName("responses")]
    public Dictionary<string, Response>? Responses { get; set; } 

    [JsonPropertyName("callbacks")]
    public Dictionary<string, Dictionary<string, PathItem>>? Callbacks { get; set; } 

    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; } 

    [JsonPropertyName("security")]
    public List<Dictionary<string, List<string>>>? Security { get; set; } 

    [JsonPropertyName("servers")]
    public List<Server>? Servers { get; set; } 
}