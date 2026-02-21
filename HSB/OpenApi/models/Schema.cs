using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Schema
{
    public Schema(){}
    public Schema(string title, Dictionary<string, Schema> properties)
    {
        this.Title = title;
        this.Properties = properties;
    }
    
    public Schema(string? @ref,
        string title,
        double? multipleOf,
        double? maximum,
        bool? exclusiveMaximum,
        double? minimum,
        bool? exclusiveMinimum,
        int? maxLength,
        int? minLength,
        string pattern,
        int? maxItems,
        int? minItems,
        bool? uniqueItems,
        int? maxProperties,
        int? minProperties,
        List<string> required,
        List<object> @enum,
        string type,
        List<Schema> allOf,
        List<Schema> oneOf,
        List<Schema> anyOf,
        Schema not,
        Schema items,
        Dictionary<string, Schema> properties,
        object additionalProperties,
        string description,
        string format,
        object @default,
        bool? nullable,
        Discriminator discriminator,
        bool? readOnly,
        bool? writeOnly,
        Xml xml,
        ExternalDocs externalDocs,
        object example,
        bool? deprecated)
    {
        Ref = @ref;
        Title = title;
        MultipleOf = multipleOf;
        Maximum = maximum;
        ExclusiveMaximum = exclusiveMaximum;
        Minimum = minimum;
        ExclusiveMinimum = exclusiveMinimum;
        MaxLength = maxLength;
        MinLength = minLength;
        Pattern = pattern;
        MaxItems = maxItems;
        MinItems = minItems;
        UniqueItems = uniqueItems;
        MaxProperties = maxProperties;
        MinProperties = minProperties;
        Required = required;
        Enum = @enum;
        Type = type;
        AllOf = allOf;
        OneOf = oneOf;
        AnyOf = anyOf;
        Not = not;
        Items = items;
        Properties = properties;
        AdditionalProperties = additionalProperties;
        Description = description;
        Format = format;
        Default = @default;
        Nullable = nullable;
        Discriminator = discriminator;
        ReadOnly = readOnly;
        WriteOnly = writeOnly;
        Xml = xml;
        ExternalDocs = externalDocs;
        Example = example;
        Deprecated = deprecated;
    }

    [JsonPropertyName("$ref")]
    public string? Ref { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("multipleOf")]
    public double? MultipleOf { get; set; }

    [JsonPropertyName("maximum")]
    public double? Maximum { get; set; }

    [JsonPropertyName("exclusiveMaximum")]
    public bool? ExclusiveMaximum { get; set; }

    [JsonPropertyName("minimum")]
    public double? Minimum { get; set; }

    [JsonPropertyName("exclusiveMinimum")]
    public bool? ExclusiveMinimum { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("pattern")]
    public string? Pattern { get; set; }

    [JsonPropertyName("maxItems")]
    public int? MaxItems { get; set; }

    [JsonPropertyName("minItems")]
    public int? MinItems { get; set; }

    [JsonPropertyName("uniqueItems")]
    public bool? UniqueItems { get; set; }

    [JsonPropertyName("maxProperties")]
    public int? MaxProperties { get; set; }

    [JsonPropertyName("minProperties")]
    public int? MinProperties { get; set; }

    [JsonPropertyName("required")]
    public List<string>? Required { get; set; }

    [JsonPropertyName("enum")]
    public List<object>? Enum { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("allOf")]
    public List<Schema>? AllOf { get; set; }

    [JsonPropertyName("oneOf")]
    public List<Schema>? OneOf { get; set; }

    [JsonPropertyName("anyOf")]
    public List<Schema>? AnyOf { get; set; }

    [JsonPropertyName("not")]
    public Schema? Not { get; set; }

    [JsonPropertyName("items")]
    public Schema? Items { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, Schema>? Properties { get; set; }

    [JsonPropertyName("additionalProperties")]
    public object? AdditionalProperties { get; set; } // bool or Schema

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("default")]
    public object? Default { get; set; }

    [JsonPropertyName("nullable")]
    public bool? Nullable { get; set; }

    [JsonPropertyName("discriminator")]
    public Discriminator? Discriminator { get; set; }

    [JsonPropertyName("readOnly")]
    public bool? ReadOnly { get; set; }

    [JsonPropertyName("writeOnly")]
    public bool? WriteOnly { get; set; }

    [JsonPropertyName("xml")]
    public Xml? Xml { get; set; }

    [JsonPropertyName("externalDocs")]
    public ExternalDocs? ExternalDocs { get; set; }

    [JsonPropertyName("example")]
    public object? Example { get; set; }

    [JsonPropertyName("deprecated")]
    public bool? Deprecated { get; set; }
}