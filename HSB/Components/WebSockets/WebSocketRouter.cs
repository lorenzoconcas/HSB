using System.Collections.Concurrent;

namespace HSB.Components.WebSockets;

public sealed class WebSocketRouter
{
    private readonly ConcurrentDictionary<string, WebSocketEndpoint> endpoints =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<WebSocketEndpoint> Endpoints => endpoints.Values.ToArray();
    public int ConnectionCount => endpoints.Values.Sum(endpoint => endpoint.ConnectionCount);

    public WebSocketEndpoint Map(string path, Action<WebSocketConnection> handler)
    {
        return Map(path, connection =>
        {
            handler(connection);
            return Task.CompletedTask;
        });
    }

    public WebSocketEndpoint Map(string path, Func<WebSocketConnection, Task> handler)
    {
        var endpoint = new WebSocketEndpoint(path, handler);
        if (!endpoints.TryAdd(endpoint.Path, endpoint))
        {
            throw new InvalidOperationException($"A WebSocket route is already registered for '{endpoint.Path}'");
        }

        return endpoint;
    }

    public WebSocketEndpoint? Match(string path)
    {
        endpoints.TryGetValue(NormalizePath(path), out var endpoint);
        return endpoint;
    }

    internal static string CombinePaths(string prefix, string path)
    {
        prefix = NormalizePath(prefix);
        path = NormalizePath(path);

        if (prefix == "/")
        {
            return path;
        }

        if (path == "/")
        {
            return prefix;
        }

        return NormalizePath(prefix.TrimEnd('/') + "/" + path.TrimStart('/'));
    }

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        path = path.Trim();

        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        while (path.Length > 1 && path.EndsWith('/'))
        {
            path = path[..^1];
        }

        return path;
    }
}
