using System.Collections.Concurrent;
using System.Text.Json;
using HSB;
using HSB.Components.WebSockets;

namespace StressTest;

public static class WebSocketHandler
{
    private static readonly ConcurrentDictionary<string, WebSocketConnection> Clients = new();

    public static void Register(Configuration config)
    {
        config.WebSocket("/ws", socket =>
        {
            var clientId = Guid.NewGuid().ToString();

            socket.OnOpen(() =>
            {
                if (!Clients.TryAdd(clientId, socket))
                {
                    socket.Close();
                    return;
                }

                Terminal.Info(
                    $"WebSocket connected: {clientId} | Total: {Clients.Count}"
                );

                socket.Send(JsonSerializer.Serialize(new
                {
                    type = "connected",
                    id = clientId,
                    clients = Clients.Count
                }));
            });

            socket.OnMessage(msg =>
            {
                string text = msg.Text;

                Terminal.Debug($"[{clientId}] {text}");

                var payload = JsonSerializer.Serialize(new
                {
                    type = "broadcast",
                    from = clientId,
                    message = text,
                    timestamp = DateTime.UtcNow,
                    clients = Clients.Count
                });

                foreach (var pair in Clients)
                {
                    var targetClientId = pair.Key;
                    var client = pair.Value;

                    if (!client.IsOpen)
                    {
                        Clients.TryRemove(targetClientId, out _);
                        continue;
                    }

                    try
                    {
                        client.Send(payload);
                    }
                    catch
                    {
                        Clients.TryRemove(targetClientId, out _);
                    }
                }
            });

            socket.OnClose(() =>
            {
                socket.OnMessage(null);
                Clients.TryRemove(clientId, out _);

                Terminal.Info(
                    $"WebSocket disconnected: {clientId} | Total: {Clients.Count}"
                );
            });
        });
    }
}
