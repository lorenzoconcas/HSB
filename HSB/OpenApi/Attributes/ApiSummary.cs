 namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class ApiSummary(string summary) : Attribute
{
   
    public string Summary { get; } = summary;
}