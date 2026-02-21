using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class Xml(string name, string ns, string prefix, bool? attribute, bool? wrapped)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("namespace")]
    public string Namespace { get; set; } = ns;

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = prefix;

    [JsonPropertyName("attribute")]
    public bool? Attribute { get; set; } = attribute;

    [JsonPropertyName("wrapped")]
    public bool? Wrapped { get; set; } = wrapped;
}