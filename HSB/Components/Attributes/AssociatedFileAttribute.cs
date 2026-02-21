using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AssociateFile : Attribute
{
    private readonly string filePath;
    private readonly List<HttpMethod> methods;
    private readonly List<string> customMethods;

    public AssociateFile(string filePath, HttpMethod method = HttpMethod.Get)
    {
        this.filePath = filePath;
        methods = [method];
        customMethods = [];
    }

    public AssociateFile(string filePath, HttpMethod[] methods)
    {
        this.filePath = filePath;
        this.methods = new(methods);
        customMethods = [];
    }
    public AssociateFile(string filePath, string customMethod)
    {
        this.filePath = filePath;
        methods = [];
        customMethods = [customMethod.ToUpper()];
    }
    public AssociateFile(string filePath, string[] customMethod)
    {
        this.filePath = filePath;
        methods = [];
        customMethods = new(customMethod.Select(m => m.ToUpper()));
    }

    public string FilePath => filePath;

    public bool MethodMatches(HttpMethod method)
    {
        return methods.Contains(method);
    }

    public bool CustomMethodMatches(string method)
    {
        return customMethods.Contains(method);
    }
}