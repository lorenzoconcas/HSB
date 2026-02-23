using System.Reflection;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

public enum RoutableMethodType
{
    Method,
    Delegate
}


public struct ExpressMap(string path, HttpMethod httpMethod, Delegate @delegate)
{
    public string Path = path;
    public HttpMethod HttpMethod = httpMethod;
    public Delegate Delegate = @delegate;
}


public struct RoutableMethod
{
    public string Path;
    public HttpMethod HttpMethod;
    public MethodInfo? MethodInfo;
    public Delegate? Delegate;

    public bool IsValid => !string.IsNullOrEmpty(Path) && MethodInfo != null;
    public RoutableMethodType Type => MethodInfo != null ? RoutableMethodType.Method : RoutableMethodType.Delegate;
}

public struct Map
{
    public string Path;
    public Type? Class;
    
    public List<RoutableMethod> SubRoutes;
}
