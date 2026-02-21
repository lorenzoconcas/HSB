using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class MediaType(
    Schema schema,
    object example,
    Dictionary<string, Example> examples,
    Dictionary<string, Encoding> encoding)
{
    [JsonPropertyName("schema")]
    public Schema Schema { get; set; } = schema;

    [JsonPropertyName("example")]
    public object Example { get; set; } = example;

    [JsonPropertyName("examples")]
    public Dictionary<string, Example> Examples { get; set; } = examples;

    [JsonPropertyName("encoding")]
    public Dictionary<string, Encoding> Encoding { get; set; } = encoding;
}