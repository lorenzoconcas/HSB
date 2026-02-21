using System.Reflection;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

public struct RoutableMethod
{
    public string Path;
    public HttpMethod HttpMethod;
    public MethodInfo MethodInfo;

    public bool IsValid => !string.IsNullOrEmpty(Path) && MethodInfo != null;
}

public struct Map
{
    public string Path;
    public Type Class;

    public List<RoutableMethod> SubRoutes;
}
