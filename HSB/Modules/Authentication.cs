using System.Reflection;
using HSB.Components;
using HSB.Constants;

namespace HSB.Modules;

/// <summary>
/// Specifies which authentication schemes are accepted by the endpoint.
/// </summary>
[Flags]
public enum AuthType
{
    None = 0,
    Bearer = 1,
    Basic = 2,
    ApiKey = 4,
    Custom = 8,
    All = Bearer | Basic | ApiKey | Custom
}

/// <summary>
/// Marks an endpoint as protected.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequireAuth : Attribute
{
    /// <summary>
    /// Allowed authentication types.
    /// </summary>
    public AuthType AuthType { get; }

    /// <summary>
    /// Optional role list.
    /// </summary>
    public string[] Roles { get; set; } = [];

    public RequireAuth(AuthType authType = AuthType.Bearer)
    {
        AuthType = authType;
    }
}

/// <summary>
/// Authentication configuration singleton.
/// </summary>
public sealed class AuthenticationSettings
{
    public static AuthenticationSettings Instance { get; } = new();

    /// <summary>
    /// Authorization header name.
    /// </summary>
    public string AuthorizationHeaderName { get; set; } = "Authorization";

    /// <summary>
    /// API Key header name.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-KEY";

    /// <summary>
    /// Enables Bearer authentication.
    /// </summary>
    public bool EnableBearer { get; set; } = true;

    /// <summary>
    /// Enables Basic authentication.
    /// </summary>
    public bool EnableBasic { get; set; }

    /// <summary>
    /// Enables API Key authentication.
    /// </summary>
    public bool EnableApiKey { get; set; }

    /// <summary>
    /// Enables custom authentication callback.
    /// </summary>
    public bool EnableCustom { get; set; }

    private AuthenticationSettings()
    {
    }
}

/// <summary>
/// Stores authenticated user information.
/// </summary>
public class AuthContext
{
    public string? Username { get; set; }

    public string? Token { get; set; }

    public AuthType AuthType { get; set; }

    public List<string> Roles { get; set; } = [];
}

/// <summary>
/// Authentication manager singleton.
/// </summary>
public sealed class AuthenticationManager
{
    private static readonly AuthenticationManager _instance = new();

    public static AuthenticationManager Instance => _instance;

    private readonly HashSet<string> _validBearerTokens = [];

    private readonly Dictionary<string, string> _basicUsers = [];

    private readonly HashSet<string> _apiKeys = [];

    /// <summary>
    /// Optional custom validator.
    /// </summary>
    public Func<Request, bool>? CustomValidator { get; set; }

    private AuthenticationManager()
    {
    }

    /// <summary>
    /// Registers a valid bearer token.
    /// </summary>
    public void AddBearerToken(string token)
    {
        _validBearerTokens.Add(token);
    }

    /// <summary>
    /// Registers a valid API key.
    /// </summary>
    public void AddApiKey(string apiKey)
    {
        _apiKeys.Add(apiKey);
    }

    /// <summary>
    /// Registers a valid username/password pair.
    /// </summary>
    public void AddBasicUser(string username, string password)
    {
        _basicUsers[username] = password;
    }

    /// <summary>
    /// Validates a bearer token.
    /// </summary>
    public bool ValidateBearer(string token)
    {
        return _validBearerTokens.Contains(token);
    }

    /// <summary>
    /// Validates an API key.
    /// </summary>
    public bool ValidateApiKey(string apiKey)
    {
        return _apiKeys.Contains(apiKey);
    }

    /// <summary>
    /// Validates basic credentials.
    /// </summary>
    public bool ValidateBasic(string username, string password)
    {
        return _basicUsers.TryGetValue(username, out var storedPassword)
               && storedPassword == password;
    }
}

/// <summary>
/// Authentication request interceptor.
/// </summary>
[Components.Module(
    ModuleType.RequestHandlerInterceptor,
    name: "Authentication Proxy",
    author: "The HSB Team",
    description: "Authentication and authorization middleware"
)]
public class Authentication
{
    /// <summary>
    /// Request interceptor executed before endpoint invocation.
    /// </summary>
    [ModuleInvokeMethod]
    public ModuleExitCode CheckAuth(
        Request request,
        Response response,
        MethodInfo @delegate
    )
    {
        var requireAuth =
            @delegate.GetCustomAttribute<RequireAuth>();

        if (requireAuth == null)
        {
            return ModuleExitCode.Continue;
        }

        var settings = AuthenticationSettings.Instance;
        var manager = AuthenticationManager.Instance;

        if (

            requireAuth.AuthType.HasFlag(AuthType.Bearer)
            && settings.EnableBearer
            && ValidateBearer(request, manager)
 ||
 requireAuth.AuthType.HasFlag(AuthType.Basic)
            && settings.EnableBasic
            && ValidateBasic(request, manager)
 ||
 requireAuth.AuthType.HasFlag(AuthType.ApiKey)
            && settings.EnableApiKey
            && ValidateApiKey(request, manager, settings)
 ||
 requireAuth.AuthType.HasFlag(AuthType.Custom)
            && settings.EnableCustom
            && manager.CustomValidator != null
            && manager.CustomValidator(request)

        )
        {
            return ModuleExitCode.Success;
        }

        response.Json(new
        {
            error = "Unauthorized"
        }, HttpCodes.UNAUTHORIZED);

        return ModuleExitCode.Reject;
    }

    /// <summary>
    /// Validates Bearer authentication.
    /// </summary>
    private bool ValidateBearer(
        Request request,
        AuthenticationManager manager
    )
    {
        if (!request.Headers.TryGetValue("Authorization", out var value))
        {
            return false;
        }

        if (!value.StartsWith("Bearer "))
        {
            return false;
        }

        var token = value["Bearer ".Length..].Trim();

        return manager.ValidateBearer(token);
    }

    /// <summary>
    /// Validates Basic authentication.
    /// </summary>
    private static bool ValidateBasic(
        Request request,
        AuthenticationManager manager
    )
    {
        if (!request.Headers.TryGetValue("Authorization", out var value))
        {
            return false;
        }

        if (!value.StartsWith("Basic "))
        {
            return false;
        }

        try
        {
            var base64 = value["Basic ".Length..];

            var decoded =
                System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(base64)
                );

            var split = decoded.Split(':', 2);

            return split.Length == 2 && manager.ValidateBasic(split[0], split[1]);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates API Key authentication.
    /// </summary>
    private bool ValidateApiKey(
        Request request,
        AuthenticationManager manager,
        AuthenticationSettings settings
    )
    {
        return request.Headers.TryGetValue(
            settings.ApiKeyHeaderName,
            out var apiKey
        ) && manager.ValidateApiKey(apiKey);
    }
}