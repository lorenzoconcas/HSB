using HSB.OpenApi.models;

namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ApiResponse(int statusCode, string description, string? responseType = null)
    : Attribute
{
    public int StatusCode { get; } = statusCode;
    public string Description { get; } = description;
    public string? ResponseType { get; } = responseType;
}