using System.Reflection;
using HSB.Constants;

namespace HSB;

public struct MethodRoute
{
    public string Path;
    public HTTP_METHOD HttpMethod;
    public MethodInfo MethodInfo;
}

public struct Map
{
    public string Path;
    public Type Class;

    public List<MethodRoute> SubRoutes;
}
