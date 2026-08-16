using System.Text;
using System.Net.Sockets;
using System.Net;
using HSB.Components.WebSockets;
using HSB.Components;
using HSB.Http;
using HSB.Constants.WebSocket;

var tests = new List<(string Name, Action Test)>
{
    ("Frame roundtrip small text", FrameRoundtripSmallText),
    ("Frame roundtrip extended payload", FrameRoundtripExtendedPayload),
    ("Frame roundtrip 64-bit payload", FrameRoundtripLargePayload),
    ("Masked frame is unmasked on parse", MaskedFrameIsUnmaskedOnParse),
    ("Concatenated frames are parsed incrementally", ConcatenatedFramesAreParsedIncrementally),
    ("Incomplete frame waits for more data", IncompleteFrameWaitsForMoreData),
    ("Oversized frame is rejected", OversizedFrameIsRejected),
    ("Duplicate websocket route throws", DuplicateWebSocketRouteThrows),
    ("Configuration parses HTTP and upload limits", ConfigurationParsesHttpAndUploadLimits),
    ("Duplicate content-length invalidates request", DuplicateContentLengthInvalidatesRequest),
    ("Multipart form parses file and field", MultipartFormParsesFileAndField),
    ("Multipart rejects invalid mime type", MultipartRejectsInvalidMimeType),
    ("Multipart parses from streaming body", MultipartParsesFromStreamingBody),
    ("Http request reader parses buffered body prefix", HttpRequestReaderParsesBufferedBodyPrefix),
    ("Http request reader parses chunked body", HttpRequestReaderParsesChunkedBody),
    ("Http request reader parses chunked multipart body", HttpRequestReaderParsesChunkedMultipartBody),
    ("Http request reader rejects duplicate content-length", HttpRequestReaderRejectsDuplicateContentLength),
    ("Http request reader rejects chunked plus content-length", HttpRequestReaderRejectsChunkedWithContentLength),
};

var failures = new List<string>();

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"[PASS] {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"[FAIL] {name} -> {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine("WebSocket runtime tests failed:");
    foreach (var failure in failures)
    {
        Console.Error.WriteLine(" - " + failure);
    }

    Environment.Exit(1);
}

static void FrameRoundtripSmallText()
{
    var frame = new Frame(opcode: Opcode.TEXT);
    frame.SetPayload("hello");

    var encoded = frame.Build();
    var decoded = new Frame(encoded);

    Expect(decoded.GetOpcode() == Opcode.TEXT, "opcode");
    Expect(decoded.GetFIN(), "FIN");
    Expect(Encoding.UTF8.GetString(decoded.GetPayload()) == "hello", "payload");
}

static void FrameRoundtripExtendedPayload()
{
    var payload = Enumerable.Repeat((byte)0x42, 300).ToArray();
    var frame = new Frame(opcode: Opcode.BINARY);
    frame.SetPayload(payload);

    var decoded = new Frame(frame.Build());

    Expect(decoded.GetOpcode() == Opcode.BINARY, "opcode");
    Expect(decoded.GetPayload().SequenceEqual(payload), "payload");
}

static void FrameRoundtripLargePayload()
{
    var payload = Enumerable.Range(0, 70_000).Select(i => (byte)(i % 251)).ToArray();
    var frame = new Frame(opcode: Opcode.BINARY);
    frame.SetPayload(payload);

    var decoded = new Frame(frame.Build());

    Expect(decoded.GetOpcode() == Opcode.BINARY, "opcode");
    Expect(decoded.GetPayload().SequenceEqual(payload), "payload");
}

static void MaskedFrameIsUnmaskedOnParse()
{
    var frame = new Frame(opcode: Opcode.TEXT, mask: true);
    frame.SetPayload("masked");

    var encoded = frame.Build();
    var decoded = new Frame(encoded);

    Expect(decoded.GetMask(), "mask flag");
    Expect(Encoding.UTF8.GetString(decoded.GetPayload()) == "masked", "unmasked payload");
}

static void ConcatenatedFramesAreParsedIncrementally()
{
    var first = new Frame(opcode: Opcode.TEXT, mask: true);
    first.SetPayload("first");
    var second = new Frame(opcode: Opcode.TEXT, mask: true);
    second.SetPayload("second");

    var bytes = first.Build().Concat(second.Build()).ToArray();

    var okFirst = Frame.TryRead(bytes, 1024, out var parsedFirst, out var firstConsumed, out var firstError);
    Expect(okFirst && firstError == null && parsedFirst != null, "first frame parsed");
    Expect(Encoding.UTF8.GetString(parsedFirst!.GetPayload()) == "first", "first payload");

    var remaining = bytes[firstConsumed..];
    var okSecond = Frame.TryRead(remaining, 1024, out var parsedSecond, out var secondConsumed, out var secondError);
    Expect(okSecond && secondError == null && parsedSecond != null, "second frame parsed");
    Expect(secondConsumed == remaining.Length, "all bytes consumed");
    Expect(Encoding.UTF8.GetString(parsedSecond!.GetPayload()) == "second", "second payload");
}

static void IncompleteFrameWaitsForMoreData()
{
    var frame = new Frame(opcode: Opcode.TEXT, mask: true);
    frame.SetPayload("partial");

    var encoded = frame.Build();
    var ok = Frame.TryRead(encoded[..3], 1024, out var parsed, out var consumed, out var error);

    Expect(!ok, "parser should wait");
    Expect(parsed == null, "parsed frame should be null");
    Expect(consumed == 0, "no bytes consumed");
    Expect(error == null, "no protocol error");
}

static void OversizedFrameIsRejected()
{
    var frame = new Frame(opcode: Opcode.BINARY, mask: true);
    frame.SetPayload(Enumerable.Repeat((byte)0xAA, 512).ToArray());

    var ok = Frame.TryRead(frame.Build(), 128, out _, out _, out var error);

    Expect(!ok, "parse result");
    Expect(error != null && error.Contains("limit", StringComparison.OrdinalIgnoreCase), "limit error");
}

static void DuplicateWebSocketRouteThrows()
{
    var router = new WebSocketRouter();
    router.Map("/chat", _ => Task.CompletedTask);

    var threw = false;
    try
    {
        router.Map("/chat", _ => Task.CompletedTask);
    }
    catch (InvalidOperationException)
    {
        threw = true;
    }

    Expect(threw, "duplicate route throw");
}

static void ConfigurationParsesHttpAndUploadLimits()
{
    const string json = """
    {
      "Address": "",
      "Port": 8080,
      "MaxConnections": 100,
      "PublicURL": "",
      "StaticFolderPath": "",
      "Debug": {
        "enabled": false,
        "verbose": false,
        "port": 8081,
        "address": "127.0.0.1",
        "logPath": "",
        "logLevel": 3
      },
      "SslSettings": {
        "SslPort": 8443,
        "PortMode": 0,
        "UpgradeUnsecureRequests": false,
        "CertificatePath": "",
        "CertificatePassword": "",
        "CheckCertificateRevocation": false,
        "ValidateClientCertificate": false,
        "ClientCertificateRequired": false,
        "TLSVersions": []
      },
      "RequestMaxSize": 1024,
      "BlockMode": 0,
      "HideBranding": true,
      "IPAutoblock": false,
      "ListeningMode": 0,
      "CustomServerName": "",
      "ServeEmbeddedResource": false,
      "EmbeddedResourcePrefix": "",
      "DefaultSessionExpirationTime": 864000000000,
      "EnabledModules": [],
      "PermanentIPList": [],
      "http": {
        "maxBodySize": "2GB",
        "maxHeaders": 120,
        "maxHeaderSize": "64KB",
        "keepAliveTimeout": 45
      },
      "upload": {
        "maxConcurrentUploads": 6,
        "tempPath": "./temp-tests",
        "maxFileSize": "512MB",
        "timeout": 120
      }
    }
    """;

    var configuration = new HSB.Configuration(json);

    Expect(configuration.Http.MaxBodySizeBytes == 2L * 1024 * 1024 * 1024, "http max body size");
    Expect(configuration.Http.MaxHeaders == 120, "http max headers");
    Expect(configuration.Http.MaxHeaderSizeBytes == 64 * 1024, "http max header size");
    Expect(configuration.Http.KeepAliveTimeoutSeconds == 45, "http keep alive timeout");
    Expect(configuration.Upload.MaxConcurrentUploads == 6, "upload concurrency");
    Expect(configuration.Upload.TempPath == "./temp-tests", "upload temp path");
    Expect(configuration.Upload.MaxFileSizeBytes == 512L * 1024 * 1024, "upload max file size");
    Expect(configuration.Upload.TimeoutSeconds == 120, "upload timeout");
}

static void DuplicateContentLengthInvalidatesRequest()
{
    using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    var configuration = new HSB.Configuration
    {
        Debug = new HSB.Debugger(false, false, 8081, "127.0.0.1")
    };
    var rawRequest = Encoding.UTF8.GetBytes(
        "POST /upload HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Content-Length: 5\r\n" +
        "Content-Length: 8\r\n\r\nhello");

    var request = new HSB.Request(rawRequest, socket, configuration);

    Expect(!request.IsValidRequest, "request should be invalid");
    Expect(request.InvalidStatusCode == HSB.Constants.HttpCodes.BAD_REQUEST, "invalid status code");
}

static void MultipartFormParsesFileAndField()
{
    var boundary = "----hsb-test-boundary";
    var body = Encoding.UTF8.GetBytes(
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"title\"\r\n\r\n" +
        "hello\r\n" +
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"upload\"; filename=\"hello.txt\"\r\n" +
        "Content-Type: text/plain\r\n\r\n" +
        "payload\r\n" +
        $"--{boundary}--\r\n");

    using var multipart = new MultiPartFormData(body, boundary);
    var parts = multipart.GetParts();
    var files = multipart.GetFiles();

    Expect(parts.Count == 1, "form field count");
    Expect(Encoding.UTF8.GetString(parts[0].Data) == "hello", "form field value");
    Expect(files.Count == 1, "file count");
    Expect(files[0].FileName == "hello.txt", "file name");
    Expect(files[0].GetMimeType() == "text/plain", "file mime");
    Expect(Encoding.UTF8.GetString(files[0].GetBytes()) == "payload", "file content");
}

static void MultipartRejectsInvalidMimeType()
{
    var boundary = "----hsb-invalid-mime";
    var body = Encoding.UTF8.GetBytes(
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"upload\"; filename=\"bad.bin\"\r\n" +
        "Content-Type: invalid mime\r\n\r\n" +
        "payload\r\n" +
        $"--{boundary}--\r\n");

    using var multipart = new MultiPartFormData(body, boundary);
    var threw = false;

    try
    {
        _ = multipart.GetFiles();
    }
    catch (Exception ex)
    {
        threw = ex.Message.Contains("mime", StringComparison.OrdinalIgnoreCase);
    }

    Expect(threw, "invalid mime should throw");
}

static void MultipartParsesFromStreamingBody()
{
    var boundary = "----hsb-stream-boundary";
    var body = Encoding.UTF8.GetBytes(
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"title\"\r\n\r\n" +
        "streamed\r\n" +
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"upload\"; filename=\"stream.txt\"\r\n" +
        "Content-Type: text/plain\r\n\r\n" +
        "stream payload\r\n" +
        $"--{boundary}--\r\n");

    using var source = new MemoryStream(body, writable: false);
    using var multipart = MultiPartFormData.Parse(
        source,
        boundary,
        new HSB.UploadOptions(),
        new HSB.HttpOptions(),
        leaveSourceStreamOpen: true);

    var parts = multipart.GetParts();
    var files = multipart.GetFiles();

    Expect(parts.Count == 1, "stream form field count");
    Expect(Encoding.UTF8.GetString(parts[0].Data) == "streamed", "stream form field value");
    Expect(files.Count == 1, "stream file count");
    Expect(files[0].FileName == "stream.txt", "stream file name");
    Expect(Encoding.UTF8.GetString(files[0].GetBytes()) == "stream payload", "stream file content");
}

static void HttpRequestReaderParsesBufferedBodyPrefix()
{
    var options = new HSB.HttpOptions
    {
        ReadBufferSizeBytes = 1024,
        MaxHeaderSizeBytes = 8 * 1024,
        MaxHeaders = 32,
        MaxRequestLineSizeBytes = 1024
    };

    var payload = Encoding.UTF8.GetBytes(
        "POST /demo HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Content-Length: 11\r\n\r\n" +
        "hello world");

    using var transport = new FakeTransportConnection(payload);
    using var readResult = new HttpRequestReader(options).ReadAsync(transport).GetAwaiter().GetResult();

    Expect(readResult != null, "reader result");
    Expect(readResult!.Head.ContentLength == 11, "content length");

    using var reader = new StreamReader(readResult.BodyStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
        bufferSize: 1024, leaveOpen: true);
    var body = reader.ReadToEnd();
    Expect(body == "hello world", "body");
}

static void HttpRequestReaderParsesChunkedBody()
{
    var options = new HSB.HttpOptions
    {
        ReadBufferSizeBytes = 1024,
        MaxHeaderSizeBytes = 8 * 1024,
        MaxHeaders = 32,
        MaxRequestLineSizeBytes = 1024,
        MaxBodySizeBytes = 64 * 1024
    };

    var payload = Encoding.UTF8.GetBytes(
        "POST /chunked HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Transfer-Encoding: chunked\r\n\r\n" +
        "5\r\nhello\r\n6\r\n world\r\n0\r\n\r\n");

    using var transport = new FakeTransportConnection(payload);
    using var readResult = new HttpRequestReader(options).ReadAsync(transport).GetAwaiter().GetResult();

    Expect(readResult != null, "chunked reader result");
    Expect(readResult!.Head.HasChunkedTransferEncoding, "chunked transfer encoding");
    Expect(readResult.Head.ContentLength == null, "chunked content length should be null");

    using var reader = new StreamReader(readResult.BodyStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
        bufferSize: 1024, leaveOpen: true);
    var body = reader.ReadToEnd();
    Expect(body == "hello world", "chunked body");
}

static void HttpRequestReaderParsesChunkedMultipartBody()
{
    var options = new HSB.HttpOptions
    {
        ReadBufferSizeBytes = 1024,
        MaxHeaderSizeBytes = 8 * 1024,
        MaxHeaders = 32,
        MaxRequestLineSizeBytes = 1024,
        MaxBodySizeBytes = 128 * 1024
    };

    var boundary = "----hsb-chunked-boundary";
    var multipartBody =
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"title\"\r\n\r\n" +
        "chunked\r\n" +
        $"--{boundary}\r\n" +
        "Content-Disposition: form-data; name=\"upload\"; filename=\"chunked.txt\"\r\n" +
        "Content-Type: text/plain\r\n\r\n" +
        "payload\r\n" +
        $"--{boundary}--\r\n";

    var chunkedBody = $"A\r\n{multipartBody[..10]}\r\n{multipartBody.Length - 10:X}\r\n{multipartBody[10..]}\r\n0\r\n\r\n";
    var payload = Encoding.UTF8.GetBytes(
        $"POST /chunked-upload HTTP/1.1\r\nHost: localhost\r\nContent-Type: multipart/form-data; boundary={boundary}\r\nTransfer-Encoding: chunked\r\n\r\n{chunkedBody}");

    using var transport = new FakeTransportConnection(payload);
    using var readResult = new HttpRequestReader(options).ReadAsync(transport).GetAwaiter().GetResult();

    Expect(readResult != null, "chunked multipart reader result");
    Expect(readResult!.Head.IsMultipartUpload, "chunked multipart flag");

    using var multipart = MultiPartFormData.Parse(
        readResult.BodyStream,
        boundary,
        new HSB.UploadOptions(),
        options,
        leaveSourceStreamOpen: true);

    var parts = multipart.GetParts();
    var files = multipart.GetFiles();

    Expect(parts.Count == 1, "chunked multipart field count");
    Expect(Encoding.UTF8.GetString(parts[0].Data) == "chunked", "chunked multipart field value");
    Expect(files.Count == 1, "chunked multipart file count");
    Expect(files[0].FileName == "chunked.txt", "chunked multipart file name");
    Expect(Encoding.UTF8.GetString(files[0].GetBytes()) == "payload", "chunked multipart file content");
}

static void HttpRequestReaderRejectsDuplicateContentLength()
{
    var options = new HSB.HttpOptions
    {
        ReadBufferSizeBytes = 1024,
        MaxHeaderSizeBytes = 8 * 1024,
        MaxHeaders = 32,
        MaxRequestLineSizeBytes = 1024
    };

    var payload = Encoding.UTF8.GetBytes(
        "POST /demo HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Content-Length: 5\r\n" +
        "Content-Length: 11\r\n\r\n" +
        "hello world");

    using var transport = new FakeTransportConnection(payload);
    var threw = false;

    try
    {
        _ = new HttpRequestReader(options).ReadAsync(transport).GetAwaiter().GetResult();
    }
    catch (HttpRequestRejectedException ex)
    {
        threw = true;
        Expect(ex.StatusCode == HSB.Constants.HttpCodes.BAD_REQUEST, "status code");
    }

    Expect(threw, "duplicate header should be rejected");
}

static void HttpRequestReaderRejectsChunkedWithContentLength()
{
    var options = new HSB.HttpOptions
    {
        ReadBufferSizeBytes = 1024,
        MaxHeaderSizeBytes = 8 * 1024,
        MaxHeaders = 32,
        MaxRequestLineSizeBytes = 1024
    };

    var payload = Encoding.UTF8.GetBytes(
        "POST /bad HTTP/1.1\r\n" +
        "Host: localhost\r\n" +
        "Content-Length: 5\r\n" +
        "Transfer-Encoding: chunked\r\n\r\n" +
        "0\r\n\r\n");

    using var transport = new FakeTransportConnection(payload);
    var threw = false;

    try
    {
        _ = new HttpRequestReader(options).ReadAsync(transport).GetAwaiter().GetResult();
    }
    catch (HttpRequestRejectedException ex)
    {
        threw = true;
        Expect(ex.StatusCode == HSB.Constants.HttpCodes.BAD_REQUEST, "chunked+content-length status code");
    }

    Expect(threw, "chunked plus content-length should be rejected");
}

static void Expect(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Expectation failed: {label}");
    }
}

sealed class FakeTransportConnection(byte[] payload) : ITransportConnection
{
    private int offset;
    private bool closed;

    public Socket Socket { get; } = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public EndPoint? RemoteEndPoint => null;

    public ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (closed)
        {
            throw new ObjectDisposedException(nameof(FakeTransportConnection));
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (offset >= payload.Length)
        {
            return ValueTask.FromResult(0);
        }

        var length = Math.Min(buffer.Length, payload.Length - offset);
        payload.AsMemory(offset, length).CopyTo(buffer);
        offset += length;
        return ValueTask.FromResult(length);
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }

    public void SetTimeouts(int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds)
    {
    }

    public void Close()
    {
        if (closed)
        {
            return;
        }

        closed = true;
        Socket.Dispose();
    }

    public void Dispose()
    {
        Close();
    }
}
