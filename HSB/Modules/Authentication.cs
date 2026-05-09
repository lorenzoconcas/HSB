using System.Reflection;
using HSB.Components;
using HSB.Constants;

namespace HSB.Modules;

[AttributeUsage(AttributeTargets.Method)]
public class RequireAuth : Attribute
{
}

[Components.Module(ModuleType.RequestHandlerInterceptor, name: "Authentication Proxy", author: "The HSB Team")]
public class Authentication
{
    private static List<string> validTokens = [];

    public static bool HasToken(string token)
    {
        return validTokens.Contains(token);
    }

    public static void AddToken(string token)
    {
        validTokens.Add(token);
    }

    [ModuleInvokeMethod]
    public ModuleExitCode CheckAuth(Request request, Response response, MethodInfo @delegate)
    {
        var requiresAuth = @delegate.IsDefined(typeof(RequireAuth), false);

        if (!requiresAuth) return ModuleExitCode.Continue;
        
       
        if (request.Headers.ContainsKey("Authorization") && validTokens.Contains(request.Headers["Authorization"]))
        {
            return ModuleExitCode.Success;
        }
        
        response.SendCode(HttpCodes.UNAUTHORIZED);

        return ModuleExitCode.Reject;
    }
}