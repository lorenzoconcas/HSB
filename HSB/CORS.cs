using System.Text.Json;
namespace HSB;
public class CORS
{
    List<string> allowedOrigins = [];
    List<string> allowedMethods = [];
    List<string> allowedHeaders = [];
    List<string> exposedHeaders = [];

    public List<string> AllowedOrigins { get => allowedOrigins; set => allowedOrigins = value; }
    public List<string> AllowedMethods { get => allowedMethods; set => allowedMethods = value; }
    public List<string> AllowedHeaders { get => allowedHeaders; set => allowedHeaders = value; }
    public List<string> ExposedHeaders { get => exposedHeaders; set => exposedHeaders = value; }

    public CORS(List<string> origins, List<string> methods, List<string> headers, List<string> exposed)
    {
        allowedOrigins = origins;
        allowedMethods = methods;
        allowedHeaders = headers;
        exposedHeaders = exposed;
    }

    public CORS()
    {

    }

    public bool IsRequestAllowed(Request req)
    {
        return IsOriginAllowed(req.Headers["Origin"])
        && IsMethodAllowed(HttpUtils.MethodAsString(req.METHOD))
        && IsHeaderAllowed(req.Headers["Access-Control-Request-Headers"])
        && IsExposedHeaderAllowed(req.Headers["Access-Control-Request-Method"]);
    }


    public bool IsOriginAllowed(string origin)
    {

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

    public bool IsExposedHeaderAllowed(string header)
    {       
        return exposedHeaders.Contains(header);
    }

    public static CORS FromJSON(JsonElement json)
    {
        var cors = new CORS();
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
            foreach (var header in allowedHeaders.EnumerateArray())
            {
                string? headerString = header.GetString();
                if (headerString != null)
                {
                    cors.AllowedHeaders.Add(headerString);
                }
            }
        }
        if (json.TryGetProperty("exposedHeaders", out var exposedHeaders))
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