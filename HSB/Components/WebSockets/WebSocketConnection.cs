using System.Collections.Concurrent;
using System.Text;
using HSB.Constants.WebSocket;

namespace HSB.Components.WebSockets;

public sealed class WebSocketConnection
{
    private readonly List<Func<Task>> openHandlers = [];
    private readonly List<Func<WebSocketMessage, Task>> messageHandlers = [];
    private readonly List<Func<Task>> closeHandlers = [];
    private readonly List<Func<Exception, Task>> errorHandlers = [];
    private readonly object handlerSync = new();
    private readonly WebSocketEndpoint endpoint;
    private readonly WebSocket runtime;
    private int isOpen;

    internal WebSocketConnection(
        Request request,
        Response response,
        Configuration configuration,
        WebSocketEndpoint endpoint)
    {
        Id = Guid.NewGuid().ToString("N");
        Request = request;
        Response = response;
        Path = WebSocketRouter.NormalizePath(request.Url);
        Headers = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
        Query = new Dictionary<string, string>(request.Parameters, StringComparer.OrdinalIgnoreCase);
        this.endpoint = endpoint;
        runtime = new WebSocket(request, response, configuration, this);
        ConnectedAtUtc = DateTime.UtcNow;
    }

    public string Id { get; }
    public string Path { get; }
    public Request Request { get; }
    public Response Response { get; }
    public DateTime ConnectedAtUtc { get; }
    public Dictionary<string, string> Headers { get; }
    public Dictionary<string, string> Query { get; }
    public string RemoteIp => Request.ClientIp;
    public bool IsOpen => Volatile.Read(ref isOpen) == 1;

    internal WebSocket Runtime => runtime;

    public void Send(string message)
    {
        SendAsync(message).GetAwaiter().GetResult();
    }

    public Task SendAsync(string message)
    {
        return runtime.SendFrameAsync(Encoding.UTF8.GetBytes(message), Opcode.TEXT);
    }

    public void Send(byte[] data)
    {
        SendAsync(data).GetAwaiter().GetResult();
    }

    public Task SendAsync(byte[] data)
    {
        return runtime.SendFrameAsync(data, Opcode.BINARY);
    }

    public void Close()
    {
        CloseAsync().GetAwaiter().GetResult();
    }

    public Task CloseAsync()
    {
        return runtime.CloseAsync();
    }

    public void Broadcast(string message)
    {
        endpoint.BroadcastAsync(message).GetAwaiter().GetResult();
    }

    public Task BroadcastAsync(string message)
    {
        return endpoint.BroadcastAsync(message);
    }

    public void Broadcast(byte[] data)
    {
        endpoint.BroadcastAsync(data).GetAwaiter().GetResult();
    }

    public Task BroadcastAsync(byte[] data)
    {
        return endpoint.BroadcastAsync(data);
    }

    public void BroadcastExceptSelf(string message)
    {
        endpoint.BroadcastAsync(message, this).GetAwaiter().GetResult();
    }

    public Task BroadcastExceptSelfAsync(string message)
    {
        return endpoint.BroadcastAsync(message, this);
    }

    public void BroadcastExceptSelf(byte[] data)
    {
        endpoint.BroadcastAsync(data, this).GetAwaiter().GetResult();
    }

    public Task BroadcastExceptSelfAsync(byte[] data)
    {
        return endpoint.BroadcastAsync(data, this);
    }

    public void OnOpen(Action handler)
    {
        OnOpen(() =>
        {
            handler();
            return Task.CompletedTask;
        });
    }

    public void OnMessage(Action<WebSocketMessage> handler)
    {
        OnMessage(message =>
        {
            handler(message);
            return Task.CompletedTask;
        });
    }

    public void OnClose(Action handler)
    {
        OnClose(() =>
        {
            handler();
            return Task.CompletedTask;
        });
    }

    public void OnError(Action<Exception> handler)
    {
        OnError(exception =>
        {
            handler(exception);
            return Task.CompletedTask;
        });
    }

    public void OnOpen(Func<Task> handler)
    {
        AddHandler(openHandlers, handler);
    }

    public void OnMessage(Func<WebSocketMessage, Task> handler)
    {
        AddHandler(messageHandlers, handler);
    }

    public void OnClose(Func<Task> handler)
    {
        AddHandler(closeHandlers, handler);
    }

    public void OnError(Func<Exception, Task> handler)
    {
        AddHandler(errorHandlers, handler);
    }

    internal void MarkOpen()
    {
        Volatile.Write(ref isOpen, 1);
    }

    internal void MarkClosed()
    {
        Volatile.Write(ref isOpen, 0);
    }

    internal Task DispatchOpenAsync()
    {
        return DispatchAsync(GetSnapshot(openHandlers));
    }

    internal Task DispatchMessageAsync(WebSocketMessage message)
    {
        return DispatchAsync(GetSnapshot(messageHandlers), message);
    }

    internal Task DispatchCloseAsync()
    {
        return DispatchAsync(GetSnapshot(closeHandlers));
    }

    internal Task DispatchErrorAsync(Exception exception)
    {
        return DispatchAsync(GetSnapshot(errorHandlers), exception);
    }

    internal Task FailAsync(Exception exception)
    {
        return runtime.FailAsync(exception);
    }

    private void AddHandler<T>(List<T> handlers, T handler)
    {
        lock (handlerSync)
        {
            handlers.Add(handler);
        }
    }

    private T[] GetSnapshot<T>(List<T> handlers)
    {
        lock (handlerSync)
        {
            return [.. handlers];
        }
    }

    private static async Task DispatchAsync(Func<Task>[] handlers)
    {
        foreach (var handler in handlers)
        {
            await handler();
        }
    }

    private static async Task DispatchAsync<T>(Func<T, Task>[] handlers, T argument)
    {
        foreach (var handler in handlers)
        {
            await handler(argument);
        }
    }
}

public sealed class WebSocketEndpoint
{
    private readonly Func<WebSocketConnection, Task> handler;
    private readonly ConcurrentDictionary<string, WebSocketConnection> connections = new();

    internal WebSocketEndpoint(string path, Func<WebSocketConnection, Task> handler)
    {
        Path = WebSocketRouter.NormalizePath(path);
        this.handler = handler;
    }

    public string Path { get; }
    public IReadOnlyCollection<WebSocketConnection> Connections => connections.Values.ToArray();
    public int ConnectionCount => connections.Count;

    internal Task ConfigureAsync(WebSocketConnection connection)
    {
        return handler(connection);
    }

    internal void Add(WebSocketConnection connection)
    {
        connections[connection.Id] = connection;
    }

    internal void Remove(WebSocketConnection connection)
    {
        connections.TryRemove(connection.Id, out _);
    }

    public Task BroadcastAsync(string message, WebSocketConnection? except = null)
    {
        return BroadcastAsync(connection => connection.SendAsync(message), except);
    }

    public Task BroadcastAsync(byte[] data, WebSocketConnection? except = null)
    {
        return BroadcastAsync(connection => connection.SendAsync(data), except);
    }

    private async Task BroadcastAsync(Func<WebSocketConnection, Task> sender, WebSocketConnection? except)
    {
        foreach (var connection in connections.Values.ToArray())
        {
            if (connection == except)
            {
                continue;
            }

            if (!connection.IsOpen)
            {
                Remove(connection);
                continue;
            }

            try
            {
                await sender(connection);
            }
            catch (Exception ex)
            {
                await connection.FailAsync(ex);
                Remove(connection);
            }
        }
    }
}
