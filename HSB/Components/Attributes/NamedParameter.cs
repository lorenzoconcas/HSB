namespace HSB.Components.Attributes;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class NamedParameter(string name, bool required = false, bool body = false) : Attribute
{
    public string Name { get; } = name;
    /// <summary>
    /// If this parameter is set to true and a call is made without this parameter,
    /// the framework will return a 400 Bad Request response with a message indicating that the parameter is missing.
    /// </summary>
    public bool Required { get; set; } = required;
    /// <summary>
    /// If this parameter is set to true the parameter is searched in the body 
    /// </summary>
    public bool Body { get; set; } = body;
}