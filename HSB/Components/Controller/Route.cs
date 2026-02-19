using HSB.Constants;

namespace HSB.Components.Controller;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Route(string path, HTTP_METHOD method) : Attribute
{
    public readonly string Path = path;
    public readonly HTTP_METHOD Method = method;
    
}