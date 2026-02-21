using HSB.OpenApi.models;

namespace HSB.OpenApi;

public class OpenApiSettings
{
    /// <summary>
    /// Enable or disable the OpenAPI documentation.
    /// If set to false, the OpenAPI documentation will not be generated
    /// and the endpoints for the documentation will not be available.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Path to access the Swagger Page. This is the path where the OpenAPI JSON will be served.
    /// </summary>
    public string Path { get; set; } = "/api";
    /// <summary>
    /// Path of the openapi.json file. This is the path where the OpenAPI JSON will be served.
    /// </summary>
    public string FilePath { get; set; } = "/openapi.json";
    /// <summary>
    /// Basic info about the Backend
    /// </summary>
    public Info? Info { get; set; }
}