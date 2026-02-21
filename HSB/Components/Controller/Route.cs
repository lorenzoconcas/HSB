using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB.Components.Controller;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Route(string path, HttpMethod method) : Attribute
{
    public readonly string Path = path;
    public readonly HttpMethod Method = method;
    
}