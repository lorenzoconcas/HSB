using HSB.Constants;

namespace HSB;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AssociatedFile : Attribute
{
    private readonly string filePath;
    private readonly List<HTTP_METHOD> methods;
    private readonly List<string> customMethods;

    public AssociatedFile(string filePath, HTTP_METHOD method = HTTP_METHOD.GET)
    {
        this.filePath = filePath;
        this.methods = new() { method };
        customMethods = new();
    }

    public AssociatedFile(string filePath, HTTP_METHOD[] methods)
    {
        this.filePath = filePath;
        this.methods = new(methods);
        customMethods = new();
    }
    public AssociatedFile(string filePath, string customMethod)
    {
        this.filePath = filePath;
        this.methods = new();
        this.customMethods = new() { customMethod.ToUpper() };
    }
    public AssociatedFile(string filePath, string[] customMethod)
    {
        this.filePath = filePath;
        this.methods = new();
        this.customMethods = new(customMethod.Select(m => m.ToUpper()));
    }

    public string FilePath => filePath;

    public bool MethodMatches(HTTP_METHOD method)
    {
        return this.methods.Contains(method);
    }

    public bool CustomMethodMatches(string method)
    {
        return this.customMethods.Contains(method);
    }
}