using System.Text.Json;

namespace HSB;

public class Cors
{
    List<string> allowedOrigins = [];
    List<string> allowedMethods = [];
    List<string> allowedHeaders = [];
    List<string> exposedHeaders = [];

    public List<string> AllowedOrigins
    {
        get => allowedOrigins;
        set => allowedOrigins = value;
    }
    public List<string> AllowedMethods
    {
        get => allowedMethods;
        set => allowedMethods = value;
    }
    public List<string> AllowedHeaders
    {
        get => allowedHeaders;
        set => allowedHeaders = value;
    }
    public List<string> ExposedHeaders
    {
        get => exposedHeaders;
        set => exposedHeaders = value;
    }

    public Cors(
        List<string> origins,
        List<string> methods,
        List<string> headers,
        List<string> exposed
    )
    {
        allowedOrigins = origins;
        allowedMethods = methods;
        allowedHeaders = headers;
        exposedHeaders = exposed;
    }

    public Cors() { }

    public bool IsRequestAllowed(Request req)
    {
        return IsOriginAllowed(req.Headers["Origin"])
            || IsMethodAllowed(HttpUtils.MethodAsString(req.Method))
            || IsHeaderAllowed(req, "Access-Control-Request-Headers")
            || IsExposedHeaderAllowed(req, "Access-Control-Request-Method");
    }

    public bool IsOriginAllowed(string origin)
    {
        if (allowedOrigins.Contains("*"))
            return true;
        return allowedOrigins.Contains(origin);
    }

    public bool IsMethodAllowed(string method)
    {
        return allowedMethods.Contains(method);
    }

    public bool IsHeaderAllowed(string header)
    {
        return allowedHeaders.Contains(header);
    }

    public bool IsHeaderAllowed(Request req, string header)
    {
        if (req.Headers.ContainsKey(header))
        {
            return exposedHeaders.Contains(header);
        }
        return false;
    }

    public bool IsExposedHeaderAllowed(string header)
    {
        return exposedHeaders.Contains(header);
    }

    public bool IsExposedHeaderAllowed(Request req, string header)
    {
        if (req.Headers.ContainsKey(header))
        {
            return allowedHeaders.Contains(header);
        }
        return false;
    }

    public static Cors FromJson(JsonElement json)
    {
        var cors = new Cors();
        if (json.TryGetProperty("allowedOrigins", out var allowedOrigins))
        {
            foreach (var origin in allowedOrigins.EnumerateArray())
            {
                string? originString = origin.GetString();
                if (originString != null)
                {
                    cors.AllowedOrigins.Add(originString);
                }
            }
        }
        if (json.TryGetProperty("allowedMethods", out var allowedMethods))
        {
            foreach (var method in allowedMethods.EnumerateArray())
            {
                string? methodString = method.GetString();
                if (methodString != null)
                {
                    cors.AllowedMethods.Add(methodString);
                }
            }
        }
        if (json.TryGetProperty("allowedHeaders", out var allowedHeaders))
        {
            foreach (
                var headerString in allowedHeaders
                    .EnumerateArray()
                    .Select(header => header.GetString())
                    .OfType<string>()
            )
            {
                cors.AllowedHeaders.Add(headerString);
            }
        }

        if (!json.TryGetProperty("exposedHeaders", out var exposedHeaders))
            return cors;
        {
            foreach (var header in exposedHeaders.EnumerateArray())
            {
                string? headerString = header.GetString();
                if (headerString != null)
                {
                    cors.ExposedHeaders.Add(headerString);
                }
            }
        }

        return cors;
    }
}
