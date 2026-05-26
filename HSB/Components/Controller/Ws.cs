namespace HSB.Components.Controller;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Ws(string path) : Attribute
{
    public readonly string Path = path;
}
