namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ApiDescription(string description) : Attribute
{
   
    public string Description { get; } = description;
}