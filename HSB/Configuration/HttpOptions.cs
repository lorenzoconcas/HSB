using System.Text.Json;
using HSB.Utils;

namespace HSB;

public sealed class HttpOptions
{
    public long MaxBodySizeBytes { get; set; } = 64L * 1024L * 1024L;
    public int MaxHeaders { get; set; } = 100;
    public int MaxHeaderSizeBytes { get; set; } = 64 * Configuration.KILOBYTE;
    public int MaxRequestLineSizeBytes { get; set; } = 8 * Configuration.KILOBYTE;
    public int KeepAliveTimeoutSeconds { get; set; } = 30;
    public int HeaderReadTimeoutSeconds { get; set; } = 15;
    public int BodyReadTimeoutSeconds { get; set; } = 300;
    public int ReadBufferSizeBytes { get; set; } = 16 * Configuration.KILOBYTE;

    public static HttpOptions FromJson(JsonElement json, int legacyRequestMaxSize = 0)
    {
        var options = new HttpOptions();

        if (legacyRequestMaxSize > 0)
        {
            options.MaxBodySizeBytes = Math.Max(options.MaxBodySizeBytes, legacyRequestMaxSize);
        }

        if (TryGetProperty(json, "maxBodySize", out var maxBodySizeElement))
        {
            options.MaxBodySizeBytes = ReadByteValue(maxBodySizeElement, options.MaxBodySizeBytes);
        }

        if (TryGetProperty(json, "maxHeaders", out var maxHeadersElement))
        {
            options.MaxHeaders = maxHeadersElement.GetInt32();
        }

        if (TryGetProperty(json, "maxHeaderSize", out var maxHeaderSizeElement))
        {
            options.MaxHeaderSizeBytes = SizeUtils.ClampToInt(
                ReadByteValue(maxHeaderSizeElement, options.MaxHeaderSizeBytes),
                1024);
        }

        if (TryGetProperty(json, "maxRequestLineSize", out var maxRequestLineSizeElement))
        {
            options.MaxRequestLineSizeBytes = SizeUtils.ClampToInt(
                ReadByteValue(maxRequestLineSizeElement, options.MaxRequestLineSizeBytes),
                256);
        }

        if (TryGetProperty(json, "keepAliveTimeout", out var keepAliveTimeoutElement))
        {
            options.KeepAliveTimeoutSeconds = keepAliveTimeoutElement.GetInt32();
        }

        if (TryGetProperty(json, "headerReadTimeout", out var headerReadTimeoutElement))
        {
            options.HeaderReadTimeoutSeconds = headerReadTimeoutElement.GetInt32();
        }

        if (TryGetProperty(json, "bodyReadTimeout", out var bodyReadTimeoutElement))
        {
            options.BodyReadTimeoutSeconds = bodyReadTimeoutElement.GetInt32();
        }

        if (TryGetProperty(json, "readBufferSize", out var readBufferSizeElement))
        {
            options.ReadBufferSizeBytes = SizeUtils.ClampToInt(
                ReadByteValue(readBufferSizeElement, options.ReadBufferSizeBytes),
                1024);
        }

        options.Clamp();
        return options;
    }

    internal void ApplyLegacyRequestMaxSize(int legacyRequestMaxSize)
    {
        if (legacyRequestMaxSize > 0)
        {
            MaxBodySizeBytes = Math.Max(MaxBodySizeBytes, legacyRequestMaxSize);
        }

        Clamp();
    }

    internal void Clamp()
    {
        MaxBodySizeBytes = Math.Max(Configuration.KILOBYTE, MaxBodySizeBytes);
        MaxHeaders = Math.Max(1, MaxHeaders);
        MaxHeaderSizeBytes = Math.Max(1024, MaxHeaderSizeBytes);
        MaxRequestLineSizeBytes = Math.Max(256, MaxRequestLineSizeBytes);
        KeepAliveTimeoutSeconds = Math.Max(5, KeepAliveTimeoutSeconds);
        HeaderReadTimeoutSeconds = Math.Max(1, HeaderReadTimeoutSeconds);
        BodyReadTimeoutSeconds = Math.Max(HeaderReadTimeoutSeconds, BodyReadTimeoutSeconds);
        ReadBufferSizeBytes = Math.Max(1024, ReadBufferSizeBytes);
    }

    private static bool TryGetProperty(JsonElement json, string camelName, out JsonElement value)
    {
        var pascalName = char.ToUpperInvariant(camelName[0]) + camelName[1..];
        return json.TryGetProperty(camelName, out value) || json.TryGetProperty(pascalName, out value);
    }

    private static long ReadByteValue(JsonElement value, long fallbackValue)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => SizeUtils.ParseBytes(value.GetString(), fallbackValue),
            JsonValueKind.Number => value.TryGetInt64(out var numericValue) ? numericValue : fallbackValue,
            _ => fallbackValue
        };
    }
}
