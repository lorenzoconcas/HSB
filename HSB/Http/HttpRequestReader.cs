using System.Buffers;
using System.Net.Sockets;
using System.Text;
using HSB.Constants;

namespace HSB.Http;

internal sealed class HttpRequestReader(HttpOptions options)
{
    private static readonly byte[] HeaderDelimiter = "\r\n\r\n"u8.ToArray();
    private static readonly HashSet<string> NonRepeatableHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Content-Length",
        "Connection",
        "Upgrade",
        "Sec-WebSocket-Key",
        "Sec-WebSocket-Version",
        "Transfer-Encoding"
    };

    public async Task<HttpRequestReadResult?> ReadAsync(ITransportConnection transport, CancellationToken cancellationToken = default)
    {
        transport.SetTimeouts(options.HeaderReadTimeoutSeconds * 1000, options.KeepAliveTimeoutSeconds * 1000);

        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(options.ReadBufferSizeBytes);
        using var headerBuffer = new PooledByteBuffer(options.ReadBufferSizeBytes, pool);

        try
        {
            while (true)
            {
                int read;
                try
                {
                    read = await transport.ReadAsync(buffer.AsMemory(0, options.ReadBufferSizeBytes), cancellationToken);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    throw HttpRequestRejectedException.RequestTimeout("Header receive timeout");
                }
                catch (IOException ex) when (ex.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                {
                    throw HttpRequestRejectedException.RequestTimeout("Header receive timeout");
                }

                if (read <= 0)
                {
                    transport.Close();
                    return null;
                }

                headerBuffer.Append(buffer.AsSpan(0, read));
                if (headerBuffer.Length > options.MaxHeaderSizeBytes)
                {
                    throw HttpRequestRejectedException.HeaderTooLarge("Header size limit exceeded");
                }

                var headerEndIndex = headerBuffer.IndexOf(HeaderDelimiter);
                if (headerEndIndex < 0)
                {
                    continue;
                }

                var headerLength = headerEndIndex + HeaderDelimiter.Length;
                var headerBytes = headerBuffer.CopyRangeToArray(0, headerLength);
                var initialBodyBytes = headerBuffer.Length > headerLength
                    ? headerBuffer.CopyRangeToArray(headerLength, headerBuffer.Length - headerLength)
                    : [];

                var head = ParseHead(headerBytes);
                Stream bodyStream = head.HasChunkedTransferEncoding
                    ? new ChunkedRequestBodyStream(transport, initialBodyBytes, options.MaxBodySizeBytes)
                    : new RequestBodyStream(transport, initialBodyBytes, head.ContentLength ?? 0);
                return new HttpRequestReadResult(head, headerBytes, bodyStream);
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    private HttpRequestHead ParseHead(byte[] headerBytes)
    {
        var headerText = Encoding.UTF8.GetString(headerBytes);
        var headerLines = headerText.Split("\r\n", StringSplitOptions.None);

        if (headerLines.Length == 0 || string.IsNullOrWhiteSpace(headerLines[0]))
        {
            throw HttpRequestRejectedException.BadRequest("Missing request line");
        }

        if (headerLines[0].Length > options.MaxRequestLineSizeBytes)
        {
            throw HttpRequestRejectedException.FromStatus(HttpCodes.URI_TOO_LONG, "Request line too large");
        }

        var parsedHeaderCount = 0;
        foreach (var line in headerLines.Skip(1))
        {
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            parsedHeaderCount++;
        }

        if (parsedHeaderCount > options.MaxHeaders)
        {
            throw HttpRequestRejectedException.FromStatus(HttpCodes.REQUEST_HEADER_FIELDS_TOO_LARGE, "Too many headers");
        }

        var requestLineParts = headerLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length != 3)
        {
            throw HttpRequestRejectedException.BadRequest("Malformed request line");
        }

        long? contentLength = null;
        var contentType = string.Empty;
        var transferEncoding = string.Empty;
        var parsedHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in headerLines.Skip(1))
        {
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                throw HttpRequestRejectedException.BadRequest("Malformed request header");
            }

            var headerName = line[..separatorIndex].Trim();
            var headerValue = line[(separatorIndex + 1)..].Trim();

            if (!parsedHeaders.TryAdd(headerName, headerValue))
            {
                if (NonRepeatableHeaders.Contains(headerName))
                {
                    throw HttpRequestRejectedException.BadRequest($"Duplicate header not allowed: {headerName}");
                }

                parsedHeaders[headerName] = string.Concat(parsedHeaders[headerName], ", ", headerValue);
                continue;
            }

            if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(headerValue, out var parsedContentLength))
                {
                    throw HttpRequestRejectedException.BadRequest("Invalid Content-Length");
                }

                contentLength = parsedContentLength;
            }

            if (headerName.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                contentType = headerValue;
            }

            if (headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                transferEncoding = headerValue;
            }
        }

        if (contentLength < 0)
        {
            throw HttpRequestRejectedException.BadRequest("Invalid Content-Length");
        }

        var isMultipartUpload = contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        var hasChunkedTransferEncoding = transferEncoding.Contains("chunked", StringComparison.OrdinalIgnoreCase);
        var hasTransferEncoding = !string.IsNullOrWhiteSpace(transferEncoding);

        if (hasChunkedTransferEncoding && contentLength.HasValue)
        {
            throw HttpRequestRejectedException.BadRequest("Chunked requests cannot include Content-Length");
        }

        if (hasTransferEncoding && !hasChunkedTransferEncoding)
        {
            throw HttpRequestRejectedException.FromStatus(HttpCodes.NOT_IMPLEMENTED, "Unsupported Transfer-Encoding");
        }

        return new HttpRequestHead(contentLength, contentType, isMultipartUpload, hasChunkedTransferEncoding);
    }
}

internal sealed record HttpRequestHead(long? ContentLength, string ContentType, bool IsMultipartUpload, bool HasChunkedTransferEncoding)
{
    public static HttpRequestHead Empty { get; } = new(null, string.Empty, false, false);
}

internal sealed class HttpRequestReadResult(HttpRequestHead head, byte[] headerBytes, Stream bodyStream) : IDisposable
{
    public HttpRequestHead Head { get; } = head;
    public byte[] HeaderBytes { get; } = headerBytes;
    public Stream BodyStream { get; } = bodyStream;

    public void Dispose()
    {
        BodyStream.Dispose();
    }
}

internal sealed class HttpRequestRejectedException(int statusCode, string reason) : Exception(reason)
{
    public int StatusCode { get; } = statusCode;

    public static HttpRequestRejectedException FromStatus(int statusCode, string reason)
    {
        return new HttpRequestRejectedException(statusCode, reason);
    }

    public static HttpRequestRejectedException BadRequest(string reason)
    {
        return FromStatus(HttpCodes.BAD_REQUEST, reason);
    }

    public static HttpRequestRejectedException RequestTimeout(string reason)
    {
        return FromStatus(HttpCodes.REQUEST_TIMEOUT, reason);
    }

    public static HttpRequestRejectedException HeaderTooLarge(string reason)
    {
        return FromStatus(HttpCodes.REQUEST_HEADER_FIELDS_TOO_LARGE, reason);
    }
}
