using System.Text.Json;

namespace HSB;

public sealed class SecurityOptions
{
    public ResponseSecurityHeadersOptions Headers { get; set; } = new();
    public RequestValidationOptions Validation { get; set; } = new();
    public RateLimitOptions RateLimit { get; set; } = new();

    public static SecurityOptions FromJson(JsonElement json)
    {
        var options = new SecurityOptions();

        if (TryGetProperty(json, "headers", out var headers))
        {
            options.Headers = ResponseSecurityHeadersOptions.FromJson(headers);
        }

        if (TryGetProperty(json, "validation", out var validation))
        {
            options.Validation = RequestValidationOptions.FromJson(validation);
        }

        if (TryGetProperty(json, "rateLimit", out var rateLimit))
        {
            options.RateLimit = RateLimitOptions.FromJson(rateLimit);
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        Headers.Clamp();
        Validation.Clamp();
        RateLimit.Clamp();
    }

    private static bool TryGetProperty(JsonElement json, string camelName, out JsonElement value)
    {
        var pascalName = char.ToUpperInvariant(camelName[0]) + camelName[1..];
        return json.TryGetProperty(camelName, out value) || json.TryGetProperty(pascalName, out value);
    }
}

public sealed class ResponseSecurityHeadersOptions
{
    public bool Enabled { get; set; }
    public bool AddContentTypeOptionsHeader { get; set; } = true;
    public bool AddFrameOptionsHeader { get; set; } = true;
    public string FrameOptionsValue { get; set; } = "DENY";
    public bool AddReferrerPolicyHeader { get; set; } = true;
    public string ReferrerPolicyValue { get; set; } = "no-referrer";
    public bool AddPermissionsPolicyHeader { get; set; }
    public string PermissionsPolicyValue { get; set; } = "geolocation=(), microphone=(), camera=()";
    public bool AddCrossOriginOpenerPolicyHeader { get; set; }
    public string CrossOriginOpenerPolicyValue { get; set; } = "same-origin";
    public bool AddCrossOriginResourcePolicyHeader { get; set; }
    public string CrossOriginResourcePolicyValue { get; set; } = "same-origin";
    public bool AddStrictTransportSecurityHeader { get; set; }
    public string StrictTransportSecurityValue { get; set; } = "max-age=31536000; includeSubDomains";
    public Dictionary<string, string> CustomHeaders { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static ResponseSecurityHeadersOptions FromJson(JsonElement json)
    {
        var options = new ResponseSecurityHeadersOptions();

        if (TryGetProperty(json, nameof(Enabled), out var enabled))
        {
            options.Enabled = enabled.GetBoolean();
        }

        if (TryGetProperty(json, nameof(AddContentTypeOptionsHeader), out var addContentTypeOptionsHeader))
        {
            options.AddContentTypeOptionsHeader = addContentTypeOptionsHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(AddFrameOptionsHeader), out var addFrameOptionsHeader))
        {
            options.AddFrameOptionsHeader = addFrameOptionsHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(FrameOptionsValue), out var frameOptionsValue))
        {
            options.FrameOptionsValue = frameOptionsValue.GetString() ?? options.FrameOptionsValue;
        }

        if (TryGetProperty(json, nameof(AddReferrerPolicyHeader), out var addReferrerPolicyHeader))
        {
            options.AddReferrerPolicyHeader = addReferrerPolicyHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(ReferrerPolicyValue), out var referrerPolicyValue))
        {
            options.ReferrerPolicyValue = referrerPolicyValue.GetString() ?? options.ReferrerPolicyValue;
        }

        if (TryGetProperty(json, nameof(AddPermissionsPolicyHeader), out var addPermissionsPolicyHeader))
        {
            options.AddPermissionsPolicyHeader = addPermissionsPolicyHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(PermissionsPolicyValue), out var permissionsPolicyValue))
        {
            options.PermissionsPolicyValue = permissionsPolicyValue.GetString() ?? options.PermissionsPolicyValue;
        }

        if (TryGetProperty(json, nameof(AddCrossOriginOpenerPolicyHeader), out var addCrossOriginOpenerPolicyHeader))
        {
            options.AddCrossOriginOpenerPolicyHeader = addCrossOriginOpenerPolicyHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(CrossOriginOpenerPolicyValue), out var crossOriginOpenerPolicyValue))
        {
            options.CrossOriginOpenerPolicyValue =
                crossOriginOpenerPolicyValue.GetString() ?? options.CrossOriginOpenerPolicyValue;
        }

        if (TryGetProperty(json, nameof(AddCrossOriginResourcePolicyHeader), out var addCrossOriginResourcePolicyHeader))
        {
            options.AddCrossOriginResourcePolicyHeader = addCrossOriginResourcePolicyHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(CrossOriginResourcePolicyValue), out var crossOriginResourcePolicyValue))
        {
            options.CrossOriginResourcePolicyValue =
                crossOriginResourcePolicyValue.GetString() ?? options.CrossOriginResourcePolicyValue;
        }

        if (TryGetProperty(json, nameof(AddStrictTransportSecurityHeader), out var addStrictTransportSecurityHeader))
        {
            options.AddStrictTransportSecurityHeader = addStrictTransportSecurityHeader.GetBoolean();
        }

        if (TryGetProperty(json, nameof(StrictTransportSecurityValue), out var strictTransportSecurityValue))
        {
            options.StrictTransportSecurityValue =
                strictTransportSecurityValue.GetString() ?? options.StrictTransportSecurityValue;
        }

        if (TryGetProperty(json, nameof(CustomHeaders), out var customHeaders) &&
            customHeaders.ValueKind == JsonValueKind.Object)
        {
            foreach (var header in customHeaders.EnumerateObject())
            {
                options.CustomHeaders[header.Name] = header.Value.GetString() ?? string.Empty;
            }
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        FrameOptionsValue = string.IsNullOrWhiteSpace(FrameOptionsValue) ? "DENY" : FrameOptionsValue.Trim();
        ReferrerPolicyValue = string.IsNullOrWhiteSpace(ReferrerPolicyValue)
            ? "no-referrer"
            : ReferrerPolicyValue.Trim();
        PermissionsPolicyValue = string.IsNullOrWhiteSpace(PermissionsPolicyValue)
            ? "geolocation=(), microphone=(), camera=()"
            : PermissionsPolicyValue.Trim();
        CrossOriginOpenerPolicyValue = string.IsNullOrWhiteSpace(CrossOriginOpenerPolicyValue)
            ? "same-origin"
            : CrossOriginOpenerPolicyValue.Trim();
        CrossOriginResourcePolicyValue = string.IsNullOrWhiteSpace(CrossOriginResourcePolicyValue)
            ? "same-origin"
            : CrossOriginResourcePolicyValue.Trim();
        StrictTransportSecurityValue = string.IsNullOrWhiteSpace(StrictTransportSecurityValue)
            ? "max-age=31536000; includeSubDomains"
            : StrictTransportSecurityValue.Trim();
    }

    private static bool TryGetProperty(JsonElement json, string propertyName, out JsonElement value)
    {
        var camelName = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return json.TryGetProperty(propertyName, out value) || json.TryGetProperty(camelName, out value);
    }
}

public sealed class RequestValidationOptions
{
    public bool Enabled { get; set; }
    public bool RequireHostHeaderForHttp11 { get; set; } = true;
    public bool RejectBackslashInPath { get; set; } = true;
    public bool RejectEncodedPathTraversal { get; set; } = true;
    public int MaxPathLength { get; set; } = 2048;
    public int MaxQueryStringLength { get; set; } = 4096;
    public int MaxQueryParameterCount { get; set; } = 128;
    public int MaxCookieCount { get; set; } = 64;
    public int MaxCookieLengthBytes { get; set; } = 4096;
    public string[] AllowedHosts { get; set; } = [];

    public static RequestValidationOptions FromJson(JsonElement json)
    {
        var options = new RequestValidationOptions();

        if (TryGetProperty(json, nameof(Enabled), out var enabled))
        {
            options.Enabled = enabled.GetBoolean();
        }

        if (TryGetProperty(json, nameof(RequireHostHeaderForHttp11), out var requireHostHeaderForHttp11))
        {
            options.RequireHostHeaderForHttp11 = requireHostHeaderForHttp11.GetBoolean();
        }

        if (TryGetProperty(json, nameof(RejectBackslashInPath), out var rejectBackslashInPath))
        {
            options.RejectBackslashInPath = rejectBackslashInPath.GetBoolean();
        }

        if (TryGetProperty(json, nameof(RejectEncodedPathTraversal), out var rejectEncodedPathTraversal))
        {
            options.RejectEncodedPathTraversal = rejectEncodedPathTraversal.GetBoolean();
        }

        if (TryGetProperty(json, nameof(MaxPathLength), out var maxPathLength))
        {
            options.MaxPathLength = maxPathLength.GetInt32();
        }

        if (TryGetProperty(json, nameof(MaxQueryStringLength), out var maxQueryStringLength))
        {
            options.MaxQueryStringLength = maxQueryStringLength.GetInt32();
        }

        if (TryGetProperty(json, nameof(MaxQueryParameterCount), out var maxQueryParameterCount))
        {
            options.MaxQueryParameterCount = maxQueryParameterCount.GetInt32();
        }

        if (TryGetProperty(json, nameof(MaxCookieCount), out var maxCookieCount))
        {
            options.MaxCookieCount = maxCookieCount.GetInt32();
        }

        if (TryGetProperty(json, nameof(MaxCookieLengthBytes), out var maxCookieLengthBytes))
        {
            options.MaxCookieLengthBytes = maxCookieLengthBytes.GetInt32();
        }

        if (TryGetProperty(json, nameof(AllowedHosts), out var allowedHosts) &&
            allowedHosts.ValueKind == JsonValueKind.Array)
        {
            options.AllowedHosts = allowedHosts.EnumerateArray()
                .Select(item => item.GetString())
                .OfType<string>()
                .ToArray();
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        MaxPathLength = Math.Max(0, MaxPathLength);
        MaxQueryStringLength = Math.Max(0, MaxQueryStringLength);
        MaxQueryParameterCount = Math.Max(1, MaxQueryParameterCount);
        MaxCookieCount = Math.Max(1, MaxCookieCount);
        MaxCookieLengthBytes = Math.Max(128, MaxCookieLengthBytes);
        AllowedHosts = AllowedHosts
            .Where(host => !string.IsNullOrWhiteSpace(host))
            .Select(host => host.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryGetProperty(JsonElement json, string propertyName, out JsonElement value)
    {
        var camelName = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return json.TryGetProperty(propertyName, out value) || json.TryGetProperty(camelName, out value);
    }
}

public sealed class RateLimitOptions
{
    public bool Enabled { get; set; }
    public int PermitLimit { get; set; } = 120;
    public int BurstLimit { get; set; } = 120;
    public int RefillPeriodSeconds { get; set; } = 60;
    public int BlockDurationSeconds { get; set; }
    public bool AddResponseHeaders { get; set; } = true;
    public bool ApplyToWebSocketHandshake { get; set; } = true;
    public int MaxTrackedClients { get; set; } = 10000;
    public string[] IgnoredIps { get; set; } = [];

    public static RateLimitOptions FromJson(JsonElement json)
    {
        var options = new RateLimitOptions();

        if (TryGetProperty(json, nameof(Enabled), out var enabled))
        {
            options.Enabled = enabled.GetBoolean();
        }

        if (TryGetProperty(json, nameof(PermitLimit), out var permitLimit))
        {
            options.PermitLimit = permitLimit.GetInt32();
        }

        if (TryGetProperty(json, nameof(BurstLimit), out var burstLimit))
        {
            options.BurstLimit = burstLimit.GetInt32();
        }

        if (TryGetProperty(json, nameof(RefillPeriodSeconds), out var refillPeriodSeconds))
        {
            options.RefillPeriodSeconds = refillPeriodSeconds.GetInt32();
        }

        if (TryGetProperty(json, nameof(BlockDurationSeconds), out var blockDurationSeconds))
        {
            options.BlockDurationSeconds = blockDurationSeconds.GetInt32();
        }

        if (TryGetProperty(json, nameof(AddResponseHeaders), out var addResponseHeaders))
        {
            options.AddResponseHeaders = addResponseHeaders.GetBoolean();
        }

        if (TryGetProperty(json, nameof(ApplyToWebSocketHandshake), out var applyToWebSocketHandshake))
        {
            options.ApplyToWebSocketHandshake = applyToWebSocketHandshake.GetBoolean();
        }

        if (TryGetProperty(json, nameof(MaxTrackedClients), out var maxTrackedClients))
        {
            options.MaxTrackedClients = maxTrackedClients.GetInt32();
        }

        if (TryGetProperty(json, nameof(IgnoredIps), out var ignoredIps) &&
            ignoredIps.ValueKind == JsonValueKind.Array)
        {
            options.IgnoredIps = ignoredIps.EnumerateArray()
                .Select(item => item.GetString())
                .OfType<string>()
                .ToArray();
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        PermitLimit = Math.Max(1, PermitLimit);
        BurstLimit = Math.Max(PermitLimit, BurstLimit);
        RefillPeriodSeconds = Math.Max(1, RefillPeriodSeconds);
        BlockDurationSeconds = Math.Max(0, BlockDurationSeconds);
        MaxTrackedClients = Math.Max(256, MaxTrackedClients);
        IgnoredIps = IgnoredIps
            .Where(ip => !string.IsNullOrWhiteSpace(ip))
            .Select(ip => ip.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TryGetProperty(JsonElement json, string propertyName, out JsonElement value)
    {
        var camelName = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        return json.TryGetProperty(propertyName, out value) || json.TryGetProperty(camelName, out value);
    }
}
