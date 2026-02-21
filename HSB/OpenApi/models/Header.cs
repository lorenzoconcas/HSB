using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Header(
    string description,
    bool? required,
    bool? deprecated,
    bool? allowEmptyValue,
    string style,
    bool? explode,
    bool? allowReserved,
    Schema schema,
    object example,
    Dictionary<string, Example> examples,
    Dictionary<string, MediaType> content,
    string @ref)
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = description;

    [JsonPropertyName("required")]
    public bool? Required { get; set; } = required;

    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; } = deprecated;

    [JsonPropertyName("allowEmptyValue")]
    public bool? AllowEmptyValue { get; set; } = allowEmptyValue;

    [JsonPropertyName("style")]
    public string Style { get; set; } = style;

    [JsonPropertyName("explode")]
    public bool? Explode { get; set; } = explode;

    [JsonPropertyName("allowReserved")]
    public bool? AllowReserved { get; set; } = allowReserved;

    [JsonPropertyName("schema")]
    public Schema Schema { get; set; } = schema;

    [JsonPropertyName("example")]
    public object Example { get; set; } = example;

    [JsonPropertyName("examples")]
    public Dictionary<string, Example> Examples { get; set; } = examples;

    [JsonPropertyName("content")]
    public Dictionary<string, MediaType> Content { get; set; } = content;

    [JsonPropertyName("$ref")]
    public string Ref { get; set; } = @ref;
}