using System.Collections.Concurrent;
using System.Text.Json;
using BackofficeDemo.Backend.Infrastructure;
using BackofficeDemo.Backend.Models;
using HSB.Components.WebSockets;

namespace BackofficeDemo.Backend.WebSockets;

public static class BackofficeNotificationsHub
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> Connections = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static Task BroadcastAsync(NotificationEnvelope message)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var tasks = Connections.Values.Select(async socket =>
        {
            try
            {
                if (socket.IsOpen)
                {
                    await socket.SendAsync(payload);
                }
            }
            catch
            {
                // ignored, connection cleanup is handled by HSB runtime lifecycle.
            }
        });

        return Task.WhenAll(tasks);
    }

    public static Task ConfigureAsync(WebSocketConnection socket)
    {
        var token = socket.Query.TryGetValue("token", out var value) ? value : string.Empty;
        var session = BackofficeApplication.Current.AuthService.GetSession(token);

        if (session == null)
        {
            return socket.CloseAsync();
        }

        socket.OnOpen(async () =>
        {
            Connections[socket.Id] = socket;
            await socket.SendAsync(JsonSerializer.Serialize(new NotificationEnvelope
            {
                Type = "system.welcome",
                TimestampUtc = DateTime.UtcNow,
                Payload = new
                {
                    user = session.Username,
                    roles = session.Roles,
                    activeConnections = Connections.Count
                }
            }, JsonOptions));
        });

        socket.OnMessage(async message =>
        {
            if (!message.IsText)
            {
                return;
            }

            if (message.Text.Equals("ping", StringComparison.OrdinalIgnoreCase))
            {
                await socket.SendAsync(JsonSerializer.Serialize(new NotificationEnvelope
                {
                    Type = "system.heartbeat",
                    TimestampUtc = DateTime.UtcNow,
                    Payload = new
                    {
                        ok = true
                    }
                }, JsonOptions));
            }
        });

        socket.OnClose(() =>
        {
            Connections.TryRemove(socket.Id, out _);
        });

        return Task.CompletedTask;
    }
}
