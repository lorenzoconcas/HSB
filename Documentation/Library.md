# Library

This page explains the current core library API.

There are two fundamental components:

| Type | Purpose |
| ---- | ------- |
| `Configuration` | Holds server settings and direct route registrations |
| `Server` | Runs the HTTP/WebSocket server |

Minimal server:

```cs
using HSB;

Configuration config = new();

config.Get("/", (Response res) =>
{
    res.SendHtmlContent("<h1>Hello World</h1>");
});

new Server(config).Start();
```

## Routing

HSB supports two routing styles.

### Configuration routes

```cs
config.Get("/health", (Response res) =>
{
    res.Json(new { status = "ok" });
});

config.Post("/echo", (Request req, Response res) =>
{
    res.Send(req.Body);
});

config.Query("/search", (Request req, Response res) =>
{
    res.Json(new { query = req.Body });
});
```

The configuration class provides helpers for:

`Get`, `Post`, `Head`, `Put`, `Delete`, `Patch`, `Trace`, `Options`, `Connect`, `Query`.

`Query` implements the safe and idempotent HTTP `QUERY` method defined by
[RFC 10008](https://www.rfc-editor.org/rfc/rfc10008.html). Its input is supplied in the request body,
and clients must include `Content-Type`. Use `Response.SetHeader("Accept-Query", ...)` to advertise
the media types accepted by a resource. Cross-origin browser requests require `QUERY` in the CORS
allowed-method list.

### Controllers

```cs
using HSB.Components.Controller;

[Controller("/api")]
public class ApiController
{
    [Get("/hello")]
    private void Hello(Response res)
    {
        res.Json(new { message = "hello" });
    }

    [Query("/search")]
    private void Search(Request req, Response res)
    {
        res.Json(new { query = req.Body });
    }
}
```

Controllers can receive `Request`, `Response`, and `Configuration` as method parameters or through fields.

## WebSocket

WebSockets are registered as endpoint handlers:

```cs
config.WebSocket("/ws", socket =>
{
    socket.OnMessage(msg => socket.Send(msg.Text));
});
```

They can also be declared in controllers:

```cs
[Controller("/api")]
public class ApiController
{
    [Ws("/live")]
    private void Live(WebSocketConnection socket)
    {
        socket.OnMessage(msg => socket.Send("ok"));
    }
}
```

A complete guide is available in [WebSockets.md](./WebSockets.md).

## Removed Servlet API

The old servlet-style API has been removed. Use `Configuration` routes or controllers instead.
