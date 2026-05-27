using System.Text.Json;
using HSB.Utils;

namespace HSB;

public sealed class UploadOptions
{
    public int MaxConcurrentUploads { get; set; } = 10;
    public string TempPath { get; set; } = "./temp";
    public long MaxFileSizeBytes { get; set; } = 2L * 1024L * 1024L * 1024L;
    public long MaxFormFieldSizeBytes { get; set; } = 1024L * 1024L;
    public int TimeoutSeconds { get; set; } = 300;
    public bool RejectInvalidMimeType { get; set; } = true;

    public static UploadOptions FromJson(JsonElement json)
    {
        var options = new UploadOptions();

        if (TryGetProperty(json, "maxConcurrentUploads", out var maxConcurrentUploadsElement))
        {
            options.MaxConcurrentUploads = maxConcurrentUploadsElement.GetInt32();
        }

        if (TryGetProperty(json, "tempPath", out var tempPathElement))
        {
            options.TempPath = tempPathElement.GetString() ?? options.TempPath;
        }

        if (TryGetProperty(json, "maxFileSize", out var maxFileSizeElement))
        {
            options.MaxFileSizeBytes = ReadByteValue(maxFileSizeElement, options.MaxFileSizeBytes);
        }

        if (TryGetProperty(json, "maxFormFieldSize", out var maxFormFieldSizeElement))
        {
            options.MaxFormFieldSizeBytes = ReadByteValue(maxFormFieldSizeElement, options.MaxFormFieldSizeBytes);
        }

        if (TryGetProperty(json, "timeout", out var timeoutElement))
        {
            options.TimeoutSeconds = timeoutElement.GetInt32();
        }

        if (TryGetProperty(json, "rejectInvalidMimeType", out var rejectInvalidMimeTypeElement))
        {
            options.RejectInvalidMimeType = rejectInvalidMimeTypeElement.GetBoolean();
        }

        options.Clamp();
        return options;
    }

    internal void Clamp()
    {
        MaxConcurrentUploads = Math.Max(1, MaxConcurrentUploads);
        TempPath = string.IsNullOrWhiteSpace(TempPath) ? "./temp" : TempPath;
        MaxFileSizeBytes = Math.Max(Configuration.KILOBYTE, MaxFileSizeBytes);
        MaxFormFieldSizeBytes = Math.Max(1024L, MaxFormFieldSizeBytes);
        TimeoutSeconds = Math.Max(5, TimeoutSeconds);
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
