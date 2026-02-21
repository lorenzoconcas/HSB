namespace HSB.Components.Controller;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class Controller(string path, bool enabled = true) : Attribute
{
    public readonly string Path = path;
    public bool Enabled = enabled;
}