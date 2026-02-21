using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Parameter
{
    public Parameter(string name, string description)
    {
        Name = name;
        Description = description;
    }
    
    public Parameter(string name,
        string @in,
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
        Name = name;
        In = @in;
        Description = description;
        Required = required;
        Deprecated = deprecated;
        AllowEmptyValue = allowEmptyValue;
        Style = style;
        Explode = explode;
        AllowReserved = allowReserved;
        Schema = schema;
        Example = example;
        Examples = examples;
        Content = content;
        Ref = @ref;
    }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("in")]
    public string? In { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; }

    [JsonPropertyName("allowEmptyValue")]
    public bool? AllowEmptyValue { get; set; }

    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("explode")]
    public bool? Explode { get; set; }

    [JsonPropertyName("allowReserved")]
    public bool? AllowReserved { get; set; }

    [JsonPropertyName("schema")]
    public Schema? Schema { get; set; }

    [JsonPropertyName("example")]
    public object? Example { get; set; }

    [JsonPropertyName("examples")]
    public Dictionary<string, Example>? Examples { get; set; }

    [JsonPropertyName("content")]
    public Dictionary<string, MediaType>? Content { get; set; }

    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }
}