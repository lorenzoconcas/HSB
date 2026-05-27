namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.All)]
public class ApiTag(string tag) : Attribute
{
    
    public string Tag { get; } = tag;
}