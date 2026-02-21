using HSB.OpenApi.models;

namespace HSB.OpenApi;


public enum Mode
{
    Disabled,
    SwaggerOnly,
    FileOnly,
    Full
}



public class OpenApiSettings
{
    /// <summary>
    /// Set OpenApi mode. This determines how and if the OpenAPI documentation will be generated and served.
    /// </summary>
    public Mode Mode { get; set; } = Mode.Full;
    
    
    /// <summary>
    /// Path to access the Swagger Page. This is the path where the OpenAPI JSON will be served.
    /// </summary>
    public string Path { get; set; } = "/api";
    /// <summary>
    /// Path of the openapi.json file. This is the path on disk where the file will be saved
    /// </summary>
    public string FilePath { get; set; } = "openapi.json";
    
    
    /// <summary>
    /// Basic info about the Backend
    /// </summary>
    public Info? Info { get; set; }
}