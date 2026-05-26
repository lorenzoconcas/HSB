# WebSockets

HSB maps WebSockets as endpoint handlers. A WebSocket route can be registered directly on
`Configuration` or declared inside a controller with `[Ws]`.

## Configuration style

```cs
using HSB;

var config = new Configuration();

config.WebSocket("/ws", socket =>
{
    socket.OnOpen(() => socket.Send("connected"));

    socket.OnMessage(msg =>
    {
        socket.Send("echo:" + msg.Text);
    });

    socket.OnClose(() =>
    {
        Console.WriteLine("closed");
    });
});
```

## Controller style

```cs
using HSB.Components.Controller;
using HSB.Components.WebSockets;

[Controller("/realtime")]
public class RealtimeController
{
    [Ws("/chat")]
    public void Chat(WebSocketConnection socket)
    {
        socket.OnOpen(() => socket.Send("connected"));

        socket.OnMessage(msg =>
        {
            socket.Send("echo:" + msg.Text);
        });
    }
}
```

Controller paths are combined with the WebSocket path. The example above maps
`/realtime/chat`.

## Async handlers

```cs
config.WebSocket("/events", socket =>
{
    socket.OnMessage(async msg =>
    {
        await socket.SendAsync("received:" + msg.Text);
    });
});
```

The setup delegate itself can also be async:

```cs
config.WebSocket("/ws", async socket =>
{
    await Task.Yield();

    socket.OnMessage(async msg =>
    {
        await socket.SendAsync("echo:" + msg.Text);
    });
});
```

## WebSocketConnection

`WebSocketConnection` exposes the connection state, request context and lifecycle hooks:

| Member | Description |
| ------ | ----------- |
| `Id` | Unique connection id |
| `Path` | Requested WebSocket path |
| `Request` / `Response` | HSB request and response objects |
| `Headers` | Request headers snapshot |
| `Query` | Query string parameters snapshot |
| `IsOpen` | Whether the connection is open |
| `Send(...)` / `SendAsync(...)` | Send text or binary data |
| `Close()` / `CloseAsync()` | Close the connection |
| `OnOpen(...)` | Register open handlers |
| `OnMessage(...)` | Register message handlers |
| `OnClose(...)` | Register close handlers |
| `OnError(...)` | Register error handlers |
| `Broadcast(...)` | Send to all connections on the same route |
| `BroadcastExceptSelf(...)` | Send to all other connections on the same route |

## WebSocketMessage

Incoming messages are wrapped in `WebSocketMessage`:

| Member | Description |
| ------ | ----------- |
| `Text` | UTF-8 text for text frames |
| `Raw` | Raw payload bytes |
| `IsText` | True for text frames |
| `IsBinary` | True for binary frames |
