namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
public class ApiResponseModel(string? name = null) : Attribute
{
   
    public string? Name { get; } = name;
}