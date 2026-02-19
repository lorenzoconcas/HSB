namespace HSB.Components.Controller;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class Controller(string path) : Attribute
{
    public readonly string Path = path;
}