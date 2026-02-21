using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;
public class Link(
    string operationRef,
    string operationId,
    Dictionary<string, object> parameters,
    object requestBody,
    string description,
    Server server)
{
    [JsonPropertyName("operationRef")]
    public string OperationRef { get; set; } = operationRef;

    [JsonPropertyName("operationId")]
    public string OperationId { get; set; } = operationId;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = parameters;

    [JsonPropertyName("requestBody")]
    public object RequestBody { get; set; } = requestBody;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("server")]
    public Server Server { get; set; } = server;
}