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
        if (!req.Headers.TryGetValue("Origin", out string? origin) || string.IsNullOrWhiteSpace(origin))
        {
            // Requests without Origin are not cross-origin requests and should not be blocked.
            return true;
        }

        if (!IsOriginAllowed(origin))
        {
            return false;
        }

        if (req.Headers.TryGetValue("Access-Control-Request-Method", out string? requestedMethod)
            && !string.IsNullOrWhiteSpace(requestedMethod)
            && !IsMethodAllowed(requestedMethod))
        {
            return false;
        }

        if (req.Headers.TryGetValue("Access-Control-Request-Headers", out string? requestedHeaders)
            && !string.IsNullOrWhiteSpace(requestedHeaders))
        {
            foreach (string header in requestedHeaders.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!IsHeaderAllowed(header))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool IsOriginAllowed(string origin)
    {
        if (allowedOrigins.Count == 0 || allowedOrigins.Contains("*"))
            return true;
        return allowedOrigins.Contains(origin);
    }

    public bool IsMethodAllowed(string method)
    {
        if (allowedMethods.Count == 0 || allowedMethods.Contains("*"))
            return true;
        return allowedMethods.Contains(method);
    }

    public bool IsHeaderAllowed(string header)
    {
        if (allowedHeaders.Count == 0 || allowedHeaders.Contains("*"))
            return true;
        return allowedHeaders.Contains(header);
    }

    public bool IsHeaderAllowed(Request req, string header)
    {
        if (req.Headers.ContainsKey(header))
        {
            return IsHeaderAllowed(req.Headers[header]);
        }
        return false;
    }

    public bool IsExposedHeaderAllowed(string header)
    {
        if (exposedHeaders.Count == 0 || exposedHeaders.Contains("*"))
            return true;
        return exposedHeaders.Contains(header);
    }

    public bool IsExposedHeaderAllowed(Request req, string header)
    {
        if (req.Headers.ContainsKey(header))
        {
            return IsExposedHeaderAllowed(req.Headers[header]);
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
