using System.Text.Json.Serialization;

namespace HSB.OpenApi.models;

public class PathItem
{
    public PathItem(string summary,
        string description,
        Operation? get,
        Operation? put,
        Operation? post,
        Operation? delete,
        Operation? options,
        Operation? head,
        Operation? patch,
        Operation? trace,
        string? @ref,
        List<Server>? servers,
        List<Parameter>? parameters)
    {
        Ref = @ref;
        Summary = summary;
        Description = description;
        Get = get;
        Put = put;
        Post = post;
        Delete = delete;
        Options = options;
        Head = head;
        Patch = patch;
        Trace = trace;
        Servers = servers;
        Parameters = parameters;
    }

    public PathItem()
    {
        Summary = "";
        Description = "";
    }

    public PathItem(string summary, string description)
    {
        this.Summary = summary;
        this.Description = description;
    }


    [JsonPropertyName("$ref")] public string? Ref { get; set; }

    [JsonPropertyName("summary")] public string Summary { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("get")] public Operation? Get { get; set; }

    [JsonPropertyName("put")] public Operation? Put { get; set; }

    [JsonPropertyName("post")] public Operation? Post { get; set; }

    [JsonPropertyName("delete")] public Operation? Delete { get; set; }

    [JsonPropertyName("options")] public Operation? Options { get; set; }

    [JsonPropertyName("head")] public Operation? Head { get; set; }

    [JsonPropertyName("patch")] public Operation? Patch { get; set; }

    [JsonPropertyName("trace")] public Operation? Trace { get; set; }

    [JsonPropertyName("servers")] public List<Server>? Servers { get; set; }

    [JsonPropertyName("parameters")] public List<Parameter>? Parameters { get; set; }
}