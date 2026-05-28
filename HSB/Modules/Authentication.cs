using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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
    /// Optional role list. Access is granted when the authenticated principal has at least one of these roles.
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
    /// Authorization header name for Bearer and Basic schemes.
    /// </summary>
    public string AuthorizationHeaderName { get; set; } = "Authorization";

    /// <summary>
    /// API Key header name.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-KEY";

    /// <summary>
    /// Bearer realm used inside WWW-Authenticate responses.
    /// </summary>
    public string BearerRealm { get; set; } = "HSB";

    /// <summary>
    /// Basic realm used inside WWW-Authenticate responses.
    /// </summary>
    public string BasicRealm { get; set; } = "HSB";

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

    /// <summary>
    /// Adds WWW-Authenticate challenge headers to unauthorized responses.
    /// </summary>
    public bool WriteAuthenticateHeader { get; set; } = true;

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
    public string? ApiKey { get; set; }
    public AuthType AuthType { get; set; }
    public List<string> Roles { get; set; } = [];
    public Dictionary<string, string> Claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool HasAnyRole(IEnumerable<string> roles)
    {
        var roleSet = Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return roles.Any(role => roleSet.Contains(role));
    }

    internal AuthContext Clone()
    {
        return new AuthContext
        {
            Username = Username,
            Token = Token,
            ApiKey = ApiKey,
            AuthType = AuthType,
            Roles = [.. Roles],
            Claims = new Dictionary<string, string>(Claims, StringComparer.OrdinalIgnoreCase)
        };
    }
}

public static class AuthenticationRequestExtensions
{
    public const string AuthContextItemKey = "hsb.auth.context";

    public static void SetAuthContext(this Request request, AuthContext context)
    {
        request.SetItem(AuthContextItemKey, context);
    }

    public static bool TryGetAuthContext(this Request request, out AuthContext? context)
    {
        return request.TryGetItem(AuthContextItemKey, out context);
    }

    public static AuthContext? GetAuthContext(this Request request)
    {
        request.TryGetAuthContext(out var context);
        return context;
    }
}

/// <summary>
/// Authentication manager singleton.
/// </summary>
public sealed class AuthenticationManager
{
    private static readonly AuthenticationManager _instance = new();
    private readonly ConcurrentDictionary<string, AuthContext> _validBearerTokens =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, BasicCredential> _basicUsers =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, AuthContext> _apiKeys =
        new(StringComparer.Ordinal);

    public static AuthenticationManager Instance => _instance;

    /// <summary>
    /// Optional custom validator returning a fully populated context.
    /// </summary>
    public Func<Request, AuthContext?>? CustomContextValidator { get; set; }

    /// <summary>
    /// Backward-compatible custom validator. Prefer <see cref="CustomContextValidator"/>.
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
        AddBearerToken(token, null, null);
    }

    /// <summary>
    /// Registers a valid bearer token with associated identity data.
    /// </summary>
    public void AddBearerToken(string token, string? username, IEnumerable<string>? roles = null,
        IDictionary<string, string>? claims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        _validBearerTokens[token] = BuildContext(AuthType.Bearer, username, token, null, roles, claims);
    }

    public bool RemoveBearerToken(string token)
    {
        return _validBearerTokens.TryRemove(token, out _);
    }

    /// <summary>
    /// Registers a valid API key.
    /// </summary>
    public void AddApiKey(string apiKey)
    {
        AddApiKey(apiKey, null, null);
    }

    /// <summary>
    /// Registers a valid API key with associated identity data.
    /// </summary>
    public void AddApiKey(string apiKey, string? username, IEnumerable<string>? roles = null,
        IDictionary<string, string>? claims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        _apiKeys[apiKey] = BuildContext(AuthType.ApiKey, username, null, apiKey, roles, claims);
    }

    public bool RemoveApiKey(string apiKey)
    {
        return _apiKeys.TryRemove(apiKey, out _);
    }

    /// <summary>
    /// Registers a valid username/password pair.
    /// </summary>
    public void AddBasicUser(string username, string password)
    {
        AddBasicUser(username, password, null, null);
    }

    /// <summary>
    /// Registers a valid username/password pair with associated roles and claims.
    /// </summary>
    public void AddBasicUser(string username, string password, IEnumerable<string>? roles,
        IDictionary<string, string>? claims = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        _basicUsers[username] = new BasicCredential(password, BuildContext(AuthType.Basic, username, null, null, roles, claims));
    }

    public bool RemoveBasicUser(string username)
    {
        return _basicUsers.TryRemove(username, out _);
    }

    /// <summary>
    /// Validates a bearer token.
    /// </summary>
    public bool ValidateBearer(string token)
    {
        return TryValidateBearer(token, out _);
    }

    /// <summary>
    /// Validates an API key.
    /// </summary>
    public bool ValidateApiKey(string apiKey)
    {
        return TryValidateApiKey(apiKey, out _);
    }

    /// <summary>
    /// Validates basic credentials.
    /// </summary>
    public bool ValidateBasic(string username, string password)
    {
        return TryValidateBasic(username, password, out _);
    }

    internal bool TryValidateBearer(string token, out AuthContext? context)
    {
        if (_validBearerTokens.TryGetValue(token, out var storedContext))
        {
            context = storedContext.Clone();
            return true;
        }

        context = null;
        return false;
    }

    internal bool TryValidateApiKey(string apiKey, out AuthContext? context)
    {
        if (_apiKeys.TryGetValue(apiKey, out var storedContext))
        {
            context = storedContext.Clone();
            return true;
        }

        context = null;
        return false;
    }

    internal bool TryValidateBasic(string username, string password, out AuthContext? context)
    {
        context = null;

        if (!_basicUsers.TryGetValue(username, out var credential))
        {
            return false;
        }

        if (!FixedTimeEquals(credential.Password, password))
        {
            return false;
        }

        context = credential.Context.Clone();
        return true;
    }

    private static AuthContext BuildContext(AuthType authType, string? username, string? token, string? apiKey,
        IEnumerable<string>? roles, IDictionary<string, string>? claims)
    {
        return new AuthContext
        {
            Username = username,
            Token = token,
            ApiKey = apiKey,
            AuthType = authType,
            Roles = roles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [],
            Claims = claims != null
                ? new Dictionary<string, string>(claims, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private sealed record BasicCredential(string Password, AuthContext Context);
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
    public ModuleExitCode CheckAuth(Request request, Response response, MethodInfo @delegate)
    {
        var requireAuth = @delegate.GetCustomAttribute<RequireAuth>();
        if (requireAuth == null)
        {
            return ModuleExitCode.Continue;
        }

        var settings = AuthenticationSettings.Instance;
        var manager = AuthenticationManager.Instance;

        var authContext = Authenticate(request, requireAuth.AuthType, settings, manager);
        if (authContext == null)
        {
            WriteUnauthorizedResponse(response, requireAuth.AuthType, settings);
            return ModuleExitCode.Reject;
        }

        if (requireAuth.Roles.Length > 0 && !authContext.HasAnyRole(requireAuth.Roles))
        {
            response.Json(new
            {
                error = "Forbidden",
                reason = "insufficient_role",
                requiredRoles = requireAuth.Roles
            }, HttpCodes.FORBIDDEN);
            return ModuleExitCode.Reject;
        }

        request.SetAuthContext(authContext);
        return ModuleExitCode.Success;
    }

    private static AuthContext? Authenticate(Request request, AuthType allowedTypes, AuthenticationSettings settings,
        AuthenticationManager manager)
    {
        if (allowedTypes.HasFlag(AuthType.Bearer) &&
            settings.EnableBearer &&
            TryValidateBearer(request, manager, settings, out var bearerContext))
        {
            return bearerContext;
        }

        if (allowedTypes.HasFlag(AuthType.Basic) &&
            settings.EnableBasic &&
            TryValidateBasic(request, manager, settings, out var basicContext))
        {
            return basicContext;
        }

        if (allowedTypes.HasFlag(AuthType.ApiKey) &&
            settings.EnableApiKey &&
            TryValidateApiKey(request, manager, settings, out var apiKeyContext))
        {
            return apiKeyContext;
        }

        if (allowedTypes.HasFlag(AuthType.Custom) &&
            settings.EnableCustom &&
            TryValidateCustom(request, manager, out var customContext))
        {
            return customContext;
        }

        return null;
    }

    private static bool TryValidateBearer(Request request, AuthenticationManager manager, AuthenticationSettings settings,
        out AuthContext? authContext)
    {
        authContext = null;

        if (!request.Headers.TryGetValue(settings.AuthorizationHeaderName, out var value) ||
            string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var token = value["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token) || !manager.TryValidateBearer(token, out authContext))
        {
            return false;
        }

        authContext!.AuthType = AuthType.Bearer;
        authContext.Token = token;
        return true;
    }

    private static bool TryValidateBasic(Request request, AuthenticationManager manager, AuthenticationSettings settings,
        out AuthContext? authContext)
    {
        authContext = null;

        if (!request.Headers.TryGetValue(settings.AuthorizationHeaderName, out var value) ||
            string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var base64 = value["Basic ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(base64))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var split = decoded.Split(':', 2);
            if (split.Length != 2)
            {
                return false;
            }

            if (!manager.TryValidateBasic(split[0], split[1], out authContext))
            {
                return false;
            }

            authContext!.AuthType = AuthType.Basic;
            authContext.Username ??= split[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryValidateApiKey(Request request, AuthenticationManager manager, AuthenticationSettings settings,
        out AuthContext? authContext)
    {
        authContext = null;

        if (!request.Headers.TryGetValue(settings.ApiKeyHeaderName, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        if (!manager.TryValidateApiKey(apiKey.Trim(), out authContext))
        {
            return false;
        }

        authContext!.AuthType = AuthType.ApiKey;
        authContext.ApiKey = apiKey.Trim();
        return true;
    }

    private static bool TryValidateCustom(Request request, AuthenticationManager manager, out AuthContext? authContext)
    {
        authContext = manager.CustomContextValidator?.Invoke(request);
        if (authContext != null)
        {
            authContext.AuthType = AuthType.Custom;
            return true;
        }

        if (manager.CustomValidator?.Invoke(request) == true)
        {
            authContext = new AuthContext
            {
                AuthType = AuthType.Custom
            };
            return true;
        }

        return false;
    }

    private static void WriteUnauthorizedResponse(Response response, AuthType requiredAuthType,
        AuthenticationSettings settings)
    {
        if (settings.WriteAuthenticateHeader)
        {
            var challenges = BuildAuthenticateChallenges(requiredAuthType, settings);
            if (challenges.Count > 0)
            {
                response.SetHeader("WWW-Authenticate", string.Join(", ", challenges));
            }
        }

        response.Json(new
        {
            error = "Unauthorized"
        }, HttpCodes.UNAUTHORIZED);
    }

    private static List<string> BuildAuthenticateChallenges(AuthType requiredAuthType, AuthenticationSettings settings)
    {
        List<string> challenges = [];

        if (requiredAuthType.HasFlag(AuthType.Bearer) && settings.EnableBearer)
        {
            challenges.Add($"Bearer realm=\"{EscapeChallengeValue(settings.BearerRealm)}\"");
        }

        if (requiredAuthType.HasFlag(AuthType.Basic) && settings.EnableBasic)
        {
            challenges.Add($"Basic realm=\"{EscapeChallengeValue(settings.BasicRealm)}\", charset=\"UTF-8\"");
        }

        return challenges;
    }

    private static string EscapeChallengeValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
