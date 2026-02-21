namespace HSB.OpenApi.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class ApiParameter(string name, string description, bool required, string type)
    : Attribute
{
    public string Name = name;
    public string Description = description;
    public bool Required = required;
    public string Type = type;
}