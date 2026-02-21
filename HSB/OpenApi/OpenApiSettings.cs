using HSB.OpenApi.models;

namespace HSB.OpenApi;

public class OpenApiSettings
{
    public bool IsEnabled { get; set; } = true;
    public string Path { get; set; } = "/api";
    public string FilePath { get; set; } = "/openapi.json";
    public Info? Info { get; set; }
}