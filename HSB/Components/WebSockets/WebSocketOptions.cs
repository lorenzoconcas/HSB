using System.Text.Json;

namespace HSB.Components.WebSockets;

public sealed class WebSocketOptions
{
    public int ReceiveChunkSize { get; set; } = Configuration.KILOBYTE * 4;
    public int ReceivePollTimeoutMilliseconds { get; set; } = 5000;
    public int IdleTimeoutMilliseconds { get; set; } = 60000;
    public int HeartbeatIntervalMilliseconds { get; set; } = 15000;
    public int MaxFramePayloadBytes { get; set; } = Configuration.KILOBYTE * 64;
    public int MaxMessagePayloadBytes { get; set; } = Configuration.KILOBYTE * 256;
    public int MaxConnectionsPerEndpoint { get; set; } = 1000;
    public int MaxConnectionsTotal { get; set; } = 10000;
    public int MaxConnectionsPerIp { get; set; }
    public bool ValidateOriginWithCors { get; set; } = true;
    public bool RequireOriginHeader { get; set; }
    public bool RequireKnownSubProtocol { get; set; }
    public bool SuppressExpectedDisconnectErrors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedSubProtocols { get; set; } = [];

    public static WebSocketOptions FromJson(JsonElement json)
    {
        var options = new WebSocketOptions();

        if (json.TryGetProperty(nameof(ReceiveChunkSize), out var receiveChunkSize))
        {
            options.ReceiveChunkSize = receiveChunkSize.GetInt32();
        }

        if (json.TryGetProperty(nameof(ReceivePollTimeoutMilliseconds), out var receivePollTimeoutMilliseconds))
        {
            options.ReceivePollTimeoutMilliseconds = receivePollTimeoutMilliseconds.GetInt32();
        }

        if (json.TryGetProperty(nameof(IdleTimeoutMilliseconds), out var idleTimeoutMilliseconds))
        {
            options.IdleTimeoutMilliseconds = idleTimeoutMilliseconds.GetInt32();
        }

        if (json.TryGetProperty(nameof(HeartbeatIntervalMilliseconds), out var heartbeatIntervalMilliseconds))
        {
            options.HeartbeatIntervalMilliseconds = heartbeatIntervalMilliseconds.GetInt32();
        }

        if (json.TryGetProperty(nameof(MaxFramePayloadBytes), out var maxFramePayloadBytes))
        {
            options.MaxFramePayloadBytes = maxFramePayloadBytes.GetInt32();
        }

        if (json.TryGetProperty(nameof(MaxMessagePayloadBytes), out var maxMessagePayloadBytes))
        {
            options.MaxMessagePayloadBytes = maxMessagePayloadBytes.GetInt32();
        }

        if (json.TryGetProperty(nameof(MaxConnectionsPerEndpoint), out var maxConnectionsPerEndpoint))
        {
            options.MaxConnectionsPerEndpoint = maxConnectionsPerEndpoint.GetInt32();
        }

        if (json.TryGetProperty(nameof(MaxConnectionsTotal), out var maxConnectionsTotal))
        {
            options.MaxConnectionsTotal = maxConnectionsTotal.GetInt32();
        }

        if (json.TryGetProperty(nameof(MaxConnectionsPerIp), out var maxConnectionsPerIp))
        {
            options.MaxConnectionsPerIp = maxConnectionsPerIp.GetInt32();
        }

        if (json.TryGetProperty(nameof(ValidateOriginWithCors), out var validateOriginWithCors))
        {
            options.ValidateOriginWithCors = validateOriginWithCors.GetBoolean();
        }

        if (json.TryGetProperty(nameof(RequireOriginHeader), out var requireOriginHeader))
        {
            options.RequireOriginHeader = requireOriginHeader.GetBoolean();
        }

        if (json.TryGetProperty(nameof(RequireKnownSubProtocol), out var requireKnownSubProtocol))
        {
            options.RequireKnownSubProtocol = requireKnownSubProtocol.GetBoolean();
        }

        if (json.TryGetProperty(nameof(SuppressExpectedDisconnectErrors), out var suppressExpectedDisconnectErrors))
        {
            options.SuppressExpectedDisconnectErrors = suppressExpectedDisconnectErrors.GetBoolean();
        }

        if (json.TryGetProperty(nameof(AllowedOrigins), out var allowedOrigins) &&
            allowedOrigins.ValueKind == JsonValueKind.Array)
        {
            options.AllowedOrigins = allowedOrigins.EnumerateArray()
                .Select(item => item.GetString())
                .OfType<string>()
                .ToArray();
        }

        if (json.TryGetProperty(nameof(AllowedSubProtocols), out var allowedSubProtocols) &&
            allowedSubProtocols.ValueKind == JsonValueKind.Array)
        {
            options.AllowedSubProtocols = allowedSubProtocols.EnumerateArray()
                .Select(item => item.GetString())
                .OfType<string>()
                .ToArray();
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        ReceiveChunkSize = Math.Max(256, ReceiveChunkSize);
        ReceivePollTimeoutMilliseconds = Math.Max(250, ReceivePollTimeoutMilliseconds);
        IdleTimeoutMilliseconds = Math.Max(ReceivePollTimeoutMilliseconds, IdleTimeoutMilliseconds);
        HeartbeatIntervalMilliseconds = Math.Max(1000, Math.Min(HeartbeatIntervalMilliseconds, IdleTimeoutMilliseconds));
        MaxFramePayloadBytes = Math.Max(125, MaxFramePayloadBytes);
        MaxMessagePayloadBytes = Math.Max(MaxFramePayloadBytes, MaxMessagePayloadBytes);
        MaxConnectionsPerEndpoint = Math.Max(1, MaxConnectionsPerEndpoint);
        MaxConnectionsTotal = Math.Max(MaxConnectionsPerEndpoint, MaxConnectionsTotal);
        MaxConnectionsPerIp = Math.Max(0, MaxConnectionsPerIp);
        AllowedOrigins = AllowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        AllowedSubProtocols = AllowedSubProtocols
            .Where(protocol => !string.IsNullOrWhiteSpace(protocol))
            .Select(protocol => protocol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
