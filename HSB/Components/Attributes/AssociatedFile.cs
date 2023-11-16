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
        methods = [method];
        customMethods = [];
    }

    public AssociatedFile(string filePath, HTTP_METHOD[] methods)
    {
        this.filePath = filePath;
        this.methods = new(methods);
        customMethods = [];
    }
    public AssociatedFile(string filePath, string customMethod)
    {
        this.filePath = filePath;
        methods = [];
        customMethods = [customMethod.ToUpper()];
    }
    public AssociatedFile(string filePath, string[] customMethod)
    {
        this.filePath = filePath;
        methods = [];
        customMethods = new(customMethod.Select(m => m.ToUpper()));
    }

    public string FilePath => filePath;

    public bool MethodMatches(HTTP_METHOD method)
    {
        return methods.Contains(method);
    }

    public bool CustomMethodMatches(string method)
    {
        return customMethods.Contains(method);
    }
}