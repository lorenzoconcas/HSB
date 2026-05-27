 namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiSummary(string summary) : Attribute
{
   
    public string Summary { get; } = summary;
}