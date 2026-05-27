using HSB.Components.WebSockets;
using HSB.Constants;
using HSB.Constants.TLS;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Constants.TLS.Manual;
using HSB.Exceptions;
using HSB.Components;
using HSB.Utils;
using Index = HSB.DefaultPages.Index;

namespace HSB;

public class Server
{
    private IPAddress? _ipAddress;
    private IPEndPoint? _localEndPoint;
    private IPEndPoint? _sslLocalEndPoint;
    private readonly Configuration _config;
    private Socket _listener;
    private Socket? _sslListener;
    private TlsConnection? _tlsConnection;
    private readonly SemaphoreSlim _uploadConcurrencyLimiter;

    private X509Certificate2? _serverCertificate;

    //routing-related variables
    private List<Map> routes = [];

    public static void Main()
    {
        Terminal.Info("HSB-# has wrongfully been compiled has executable and will not run!");
        Terminal.Info("To run as standalone you must compile/execute the \"Standalone\" or the \"Launcher\" project");
        Terminal.Info("Check the documentation for more info (\"https://github.com/lorenzoconcas/HSB\")");
    }

    /// <summary>
    /// Calculates the IP Address to listen to based on configuration
    /// </summary>
    private void SetIpAddress()
    {
        if (_config.Address == "")
        {
            _ipAddress = _config.ListeningMode switch
            {
                IpMode.Ipv4 => IPAddress.Any,
                _ => IPAddress.IPv6Any,
            };
            return;
        }


        List<IPAddress> addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetwork)];

        //this fixes an error where user specifies an ipv4 address but want the server to listen BOTH or ipv6 only

        if (addresses.Count != 0)
        {
            _ipAddress = addresses.First();
            _config.ListeningMode = IpMode.Ipv4;
        }
        else
        {
            addresses = [.. Dns.GetHostAddresses(_config.Address, AddressFamily.InterNetworkV6)];
            if (addresses.Count != 0)
            {
                _ipAddress = addresses.First();
                _config.ListeningMode = IpMode.Ipv6;
            }
            else
            {
                _config.Debug.ERROR("Cannot determine address to listen to");
                Environment.Exit((int) ServerErrors.AddressNotFound);
            }
        }
    }


    /// <summary>
    /// Initializes the endpoints and listeners based on configuration
    /// </summary>
    private void SetEndpoint()
    {
        _localEndPoint = new IPEndPoint(_ipAddress!, _config.Port);
        _listener = new Socket(_ipAddress!.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);


        if (_config.ListeningMode != IpMode.Any) return;
        _listener.DualMode = true;
        if (_config.SslSettings.IsEnabled() && _sslListener != null)
            _sslListener!.DualMode = true;
    }

    /// <summary>
    /// If configuration has SSL enabled, initializes the SSL listener and endpoint
    /// </summary>
    private void SetSsl()
    {
        var sslConf = _config.SslSettings;


        //if ssl is set and configuration is set to use two ports we start the sslListener
        if ((!sslConf.IsEnabled() && !sslConf.IsDebugModeEnabled())) return;
        X509Certificate2? cert = null;
        if (sslConf.UseDebugCertificate)
        {
            _config.Debug.INFO("Server is set to use a debug certificate");
            cert = SslConfiguration.TryLoadDebugCertificate(c: _config);
            if (cert == null)
            {
                _config.Debug.ERROR("Cannot load debug certificate, server cannot start with this configuration!");
                Environment.Exit((int) ServerErrors.CannotLoadDebugCertificate);
            }
        }
        else if (sslConf.IsEnabled())
            cert = sslConf.GetCertificate();

        _serverCertificate = cert;

        if (cert != null)
        {
            _tlsConnection = new TlsConnection(
                cert,
                sslConf.GetProtocols(),
                sslConf.CheckCertificateRevocation,
                sslConf.ClientCertificateRequired
            );
        }


        if (sslConf.PortMode != SSL_PORT_MODE.DUAL_PORT) return;
        _sslLocalEndPoint = new(_ipAddress!, _config.SslSettings.SslPort);
        if (_sslLocalEndPoint == null)
        {
            _config.Debug.ERROR("Cannot create SSL endpoint");
            Environment.Exit((int) ServerErrors.CannotCreateSslEndpoint);
        }

        _sslListener = new(_ipAddress!.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        if (_sslListener != null) return;
        _config.Debug.ERROR("Cannot create SSL listener");
        Environment.Exit((int) ServerErrors.CannotCreateSslListener);
    }

    private void PrintFinalInfo()
    {
        if (_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled())
        {
            _config.Debug.INFO("Server is running in SSL mode");
        }


        var prefix = "http";
        if ((_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled()) &&
            _config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT)
        {
            _config.Debug.INFO(_config.PublicUrl == ""
                ? $"Listening at https://{_sslLocalEndPoint}/"
                : $"Listening at https://{_config.PublicUrl}:{_config.SslSettings.SslPort}/");
        }

        else if ((_config.SslSettings.IsEnabled() || _config.SslSettings.IsDebugModeEnabled()) &&
                 _config.SslSettings.PortMode == SSL_PORT_MODE.SINGLE_PORT)
            prefix += "s";

        _config.Debug.INFO(_config.PublicUrl == ""
            ? $"Listening at {prefix}://{_localEndPoint}/"
            : $"Listening at {prefix}://{_config.PublicUrl}:{_config.Port}/");

        _config.Debug.INFO("Server started");
    }

    public Server(Configuration? config = null)
    {
        _ipAddress = IPAddress.Any;
        _listener = null!;
        _sslListener = null;
        _tlsConnection = null;
        _serverCertificate = null;
        _uploadConcurrencyLimiter = new SemaphoreSlim(1, 1);

        config ??= new Configuration();

        if (!config.HideBranding)
            CliUtils.PrintLogo();

        if (config.Port == 0)
        {
            //if port is 0, we use a random port in the range 1024-65535
            config.Port = (ushort) new Random().Next(1024, 65535);
        }

        _config = config;
        _config.Http.ApplyLegacyRequestMaxSize(_config.RequestMaxSize);
        _config.Upload.Clamp();
        _uploadConcurrencyLimiter = new SemaphoreSlim(_config.Upload.MaxConcurrentUploads, _config.Upload.MaxConcurrentUploads);

        _config.ExpressRouteAdded += (r) =>
        {
            routes.Add(new Map()
            {
                Path = r.Path,
                SubRoutes =
                [
                    new RoutableMethod()
                    {
                        Delegate = r.Delegate,
                        HttpMethod = r.HttpMethod,
                        Path = "/"
                    }
                ]
            });


            _config.Debug.INFO(
                $"Route |{Terminal.FG_TO_STRING(FgColor.Green)}{r.HttpMethod}{Terminal.RESET} -> {r.Path} (Delegate)");
        };

        _config.WebSocketRouteAdded += endpoint =>
        {
            _config.Debug.INFO(
                $"WebSocket | {Terminal.FG_TO_STRING(FgColor.Green)}WS{Terminal.RESET} -> {endpoint.Path} (Delegate)");
        };


        config.Debug.INFO("Starting logging...");


        SetIpAddress();
        SetEndpoint();
        SetSsl();
        MapRoutes();
        PrintFinalInfo();


        //init modules


        _config.SetRoutes(routes);

        ExecuteModule(ModuleType.Global, _config);
        ExecuteModule(ModuleType.Service, _config);


        /*//the class will automatically set according to configuration
        new OpenApiBuilder(config, Routes).Init();*/

        //end of the server initialization
    }

    private static bool ExecuteModule(ModuleType type, Configuration config, Request? req = null, Response? res = null,
        MethodInfo? @delegate = null)
    {
        foreach (var m in InstanceManager<ModuleManager>.GetInstance().GetModules(type, config.EnabledModules))
        {
            //    var r = m.InvokeMember("Process", BindingFlags., null, null, [req, res]);

            var method = m.method;
            var parameters = method.GetParameters();
            List<object> callingParams = [];

            foreach (var field in parameters)
            {
                var parameterType = field.ParameterType;

                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType()!;
                }

                if (parameterType == typeof(Request))
                {
                    callingParams.Add(req!);
                }
                else if (parameterType == typeof(Response))
                {
                    callingParams.Add(res!);
                }
                else if (parameterType == typeof(Configuration))
                {
                    callingParams.Add(config);
                }
                else if (parameterType == typeof(MethodInfo))
                {
                    callingParams.Add(@delegate!);
                }
            }

            var instance = Activator.CreateInstance(m.type);
            var result = method.Invoke(instance, callingParams.ToArray());
            if (result is not ModuleExitCode r)
            {
                throw new InvalidModuleResponseException(m);
            }

            switch (r)
            {
                case ModuleExitCode.Reject: return false;
                case ModuleExitCode.Error:
                    Terminal.Info($"An error occured with module {m.name}");
                    return true;
                default:
                case ModuleExitCode.Success:
                case ModuleExitCode.Continue: return true;
            }
        }

        return true;
    }

    public void Start(bool openInBrowser = false)
    {
        if (_localEndPoint == null)
        {
            _config.Debug.ERROR("An error occurred while initializing the server (local endpoint is null)");
            Environment.Exit((int) ServerErrors.CannotCreateLocalEndpoint);
            return;
        }

        try
        {
            _listener.Bind(_localEndPoint);
            _listener.Listen(Math.Max(8192, Convert.ToInt32(_config.MaxConnections)));

            var sslConf = _config.SslSettings;

            if (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled())
            {
                //sslListener and sslLocalEndPoint are not null because we checked in the constructor
                if (_sslListener != null)
                {
                    _sslListener!.Bind(_sslLocalEndPoint!);
                    _sslListener.Listen(8192);
                }
            }

            OpenInBrowserIfSet(openInBrowser, sslConf.IsEnabled(),
                sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT ? _sslLocalEndPoint! : _localEndPoint);

            //this makes the second port listen to SSL requests
            if ((sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) && sslConf.PortMode == SSL_PORT_MODE.DUAL_PORT)
            {
                _ = Task.Run(() => AcceptLoopAsync(_sslListener!, true));
            }

            //since the base port is always listening this is always executed
            var sslMode = (sslConf.IsEnabled() || sslConf.IsDebugModeEnabled()) &&
                          sslConf.PortMode == SSL_PORT_MODE.SINGLE_PORT;

            AcceptLoopAsync(_listener, sslMode).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _config.Debug.ERROR(e);
        }
    }

    private static void OpenInBrowserIfSet(bool openInBrowser, bool ssl, IPEndPoint endpoint)
    {
        if (!openInBrowser) return;
        var psi = new ProcessStartInfo
        {
            FileName = $"http{(ssl ? "s" : "")}:{endpoint}",
            UseShellExecute = true
        };
        System.Diagnostics.Process.Start(psi);
    }

    private async Task AcceptLoopAsync(Socket listener, bool sslMode)
    {
        while (true)
        {
            var socket = await listener.AcceptAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    await Process(socket, sslMode);
                }
                catch (Exception e)
                {
                    _config.Debug.ERROR(e);

                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                        // ignored
                    }

                    socket.Close();
                }
            });
        }
    }

    private async Task Process(Socket socket, bool sslMode)
    {
        socket.NoDelay = true;
        socket.ReceiveTimeout = _config.Http.HeaderReadTimeoutSeconds * 1000;
        socket.SendTimeout = _config.Http.KeepAliveTimeoutSeconds * 1000;

        SslStream? sslStream = null;
        Tls12Handler? hsbTls = null;
        Request? req = null;
        RequestEnvelope? envelope = null;

        if (sslMode)
        {
            if (_config.SslSettings.SslHandler == SslHandler.Hsb)
            {
                try
                {
                    if (_serverCertificate == null)
                        throw new Exception("Server certificate is null");

                    hsbTls = new Tls12Handler(socket, _serverCertificate);
                    hsbTls.PerformHandshake();
                }
                catch (Exception e)
                {
                    _config.Debug.ERROR($"Manual TLS Handshake/Read Failed: {e.Message}");
                    CloseTransport(socket, sslStream);
                    return;
                }
            }
            else
            {
                if (_tlsConnection == null)
                {
                    _config.Debug.ERROR("SSL Mode requested but TlsConnection is not initialized");

                    try
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    {
                        // ignored
                    }

                    socket.Close();
                    return;
                }

                sslStream = _tlsConnection.EstablishSsl(socket);

                if (sslStream != null)
                {
                    try
                    {
                    }
                    catch (Exception e)
                    {
                        _config.Debug.DEBUG(e);
                        CloseTransport(socket, sslStream);
                        return;
                    }
                }
            }
        }
        else if (_config.SslSettings.IsEnabled() &&
                 _config.SslSettings.UpgradeUnsecureRequests)
        {
            _config.Debug.WARNING("Unsecure request received, redirecting to SSL");

            Request rq = new([], socket, _config, false);
            Response res = new(socket, rq, _config, null);

            var redirectEndpoint =
                _config.SslSettings.PortMode == SSL_PORT_MODE.DUAL_PORT
                    ? _sslLocalEndPoint!
                    : _localEndPoint;

            if (redirectEndpoint == null)
            {
                _config.Debug.WARNING("Cannot set redirect endpoint");
                CloseTransport(socket, sslStream);
                return;
            }

            res.Redirect("https://" + redirectEndpoint, HttpCodes.MOVED_PERMANENTLY);
            return;
        }

        try
        {
            envelope = await ReadRequestEnvelopeAsync(socket, sslStream, hsbTls);
            if (envelope == null)
            {
                return;
            }

            req = new Request(envelope.HeaderBytes, envelope.BodyBytes, envelope.BodyTempFilePath, socket, _config, sslMode);
            Response res = new(socket, req, _config, sslStream, hsbTls);

            foreach (var cookie in req.ResponseCookies)
            {
                res.AddCookie(cookie.ToString());
            }

            if (!req.IsValidRequest)
            {
                new Error(res, _config, req.InvalidReason, req.InvalidStatusCode).Throw();
                return;
            }

            await ProcessRequestAsync(req, res);
        }
        finally
        {
            req?.Dispose();
            envelope?.Dispose();
        }
    }

    private async Task<RequestEnvelope?> ReadRequestEnvelopeAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler)
    {
        var headerReadResult = await ReadHeadersAsync(socket, sslStream, tlsHandler);
        if (headerReadResult == null)
        {
            return null;
        }

        if (!TryParseRequestHead(headerReadResult.HeaderBytes, out var headInfo, out var rejectionStatusCode, out var rejectionReason))
        {
            await SendSimpleResponseAsync(socket, sslStream, tlsHandler, rejectionStatusCode, rejectionReason);
            return null;
        }

        if (headInfo.IsMultipartUpload && headInfo.HasChunkedTransferEncoding)
        {
            await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.NOT_IMPLEMENTED,
                "Streaming upload mode is postponed");
            return null;
        }

        if (headInfo.ContentLength > _config.Http.MaxBodySizeBytes)
        {
            await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.PAYLOAD_TOO_LARGE,
                "Request body too large");
            return null;
        }

        if (headInfo.ContentLength <= 0)
        {
            return new RequestEnvelope(headerReadResult.HeaderBytes, [], null, null);
        }

        if (headInfo.IsMultipartUpload)
        {
            if (!await _uploadConcurrencyLimiter.WaitAsync(0))
            {
                var remoteIp = socket.RemoteEndPoint is IPEndPoint endpoint ? endpoint.Address.ToString() : "unknown";
                _config.Debug.WARNING($"[UPLOAD][ERROR] client={remoteIp} reason=too_many_uploads");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.TOO_MANY_REQUESTS,
                    "Too many uploads");
                return null;
            }

            var uploadLease = new SemaphoreLease(_uploadConcurrencyLimiter);
            try
            {
                var tempBodyPath = await BufferRequestBodyToTempFileAsync(socket, sslStream, tlsHandler, headInfo, headerReadResult.BufferedBodyBytes);
                if (tempBodyPath == null)
                {
                    uploadLease.Dispose();
                    return null;
                }

                return new RequestEnvelope(headerReadResult.HeaderBytes, [], tempBodyPath, uploadLease);
            }
            catch
            {
                uploadLease.Dispose();
                throw;
            }
        }

        var bodyBytes = await ReadRequestBodyIntoMemoryAsync(socket, sslStream, tlsHandler, headInfo, headerReadResult.BufferedBodyBytes);
        return bodyBytes == null ? null : new RequestEnvelope(headerReadResult.HeaderBytes, bodyBytes, null, null);
    }

    private async Task<HeaderReadResult?> ReadHeadersAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler)
    {
        SetTransportTimeouts(socket, sslStream, _config.Http.HeaderReadTimeoutSeconds * 1000, _config.Http.KeepAliveTimeoutSeconds * 1000);

        var buffer = new byte[_config.Http.ReadBufferSizeBytes];
        var headerBuffer = new List<byte>(_config.Http.ReadBufferSizeBytes);
        var headerDelimiter = "\r\n\r\n"u8.ToArray();

        while (true)
        {
            int read;
            try
            {
                read = await ReadFromTransportAsync(socket, sslStream, tlsHandler, buffer);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                _config.Debug.WARNING("Closing connection: header receive timeout");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Header receive timeout");
                return null;
            }
            catch (IOException ex) when (ex.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
            {
                _config.Debug.WARNING("Closing connection: header receive timeout");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Header receive timeout");
                return null;
            }
            catch (Exception ex) when (IsExpectedDisconnect(ex))
            {
                CloseTransport(socket, sslStream);
                return null;
            }

            if (read <= 0)
            {
                CloseTransport(socket, sslStream);
                return null;
            }

            headerBuffer.AddRange(buffer[..read]);

            if (headerBuffer.Count > _config.Http.MaxHeaderSizeBytes)
            {
                _config.Debug.WARNING("Closing connection: header size limit exceeded");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_HEADER_FIELDS_TOO_LARGE,
                    "Header size limit exceeded");
                return null;
            }

            var headerEndIndex = CollectionsMarshal.AsSpan(headerBuffer).IndexOf(headerDelimiter);
            if (headerEndIndex < 0)
            {
                continue;
            }

            var headerLength = headerEndIndex + headerDelimiter.Length;
            var headerBytes = headerBuffer[..headerLength].ToArray();
            var bufferedBodyBytes = headerBuffer.Count > headerLength
                ? headerBuffer[headerLength..].ToArray()
                : [];
            return new HeaderReadResult(headerBytes, bufferedBodyBytes);
        }
    }

    private bool TryParseRequestHead(byte[] headerBytes, out RequestHeadInfo headInfo, out int rejectionStatusCode, out string rejectionReason)
    {
        var headerText = Encoding.UTF8.GetString(headerBytes);
        var headerLines = headerText.Split("\r\n", StringSplitOptions.None);

        if (headerLines.Length == 0 || string.IsNullOrWhiteSpace(headerLines[0]))
        {
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.BAD_REQUEST;
            rejectionReason = "Missing request line";
            return false;
        }

        if (headerLines[0].Length > _config.Http.MaxRequestLineSizeBytes)
        {
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.URI_TOO_LONG;
            rejectionReason = "Request line too large";
            return false;
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

        if (parsedHeaderCount > _config.Http.MaxHeaders)
        {
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.REQUEST_HEADER_FIELDS_TOO_LARGE;
            rejectionReason = "Too many headers";
            return false;
        }

        var requestLineParts = headerLines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLineParts.Length != 3)
        {
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.BAD_REQUEST;
            rejectionReason = "Malformed request line";
            return false;
        }

        long contentLength = 0;
        var contentType = string.Empty;
        var transferEncoding = string.Empty;

        foreach (var line in headerLines.Skip(1))
        {
            if (string.IsNullOrEmpty(line))
            {
                break;
            }

            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                headInfo = RequestHeadInfo.Empty;
                rejectionStatusCode = HttpCodes.BAD_REQUEST;
                rejectionReason = "Malformed request header";
                return false;
            }

            var headerName = line[..separatorIndex].Trim();
            var headerValue = line[(separatorIndex + 1)..].Trim();

            if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                !long.TryParse(headerValue, out contentLength))
            {
                headInfo = RequestHeadInfo.Empty;
                rejectionStatusCode = HttpCodes.BAD_REQUEST;
                rejectionReason = "Invalid Content-Length";
                return false;
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
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.BAD_REQUEST;
            rejectionReason = "Invalid Content-Length";
            return false;
        }

        var isMultipart = contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
        var hasChunkedTransferEncoding = transferEncoding.Contains("chunked", StringComparison.OrdinalIgnoreCase);

        if (hasChunkedTransferEncoding)
        {
            headInfo = RequestHeadInfo.Empty;
            rejectionStatusCode = HttpCodes.NOT_IMPLEMENTED;
            rejectionReason = "Chunked request bodies are not supported";
            return false;
        }

        headInfo = new RequestHeadInfo(contentLength, contentType, isMultipart, hasChunkedTransferEncoding);
        rejectionStatusCode = 0;
        rejectionReason = string.Empty;
        return true;
    }

    private async Task<byte[]?> ReadRequestBodyIntoMemoryAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler,
        RequestHeadInfo headInfo,
        byte[] bufferedBodyBytes)
    {
        SetTransportTimeouts(socket, sslStream, _config.Http.BodyReadTimeoutSeconds * 1000, _config.Http.KeepAliveTimeoutSeconds * 1000);

        var remaining = headInfo.ContentLength;
        var bodyBytes = new byte[remaining];
        var readBuffer = new byte[_config.Http.ReadBufferSizeBytes];
        var offset = 0;

        var initialBodyBytes = bufferedBodyBytes.Length > remaining
            ? bufferedBodyBytes[..(int)remaining]
            : bufferedBodyBytes;

        if (initialBodyBytes.Length > 0)
        {
            Buffer.BlockCopy(initialBodyBytes, 0, bodyBytes, 0, initialBodyBytes.Length);
            offset += initialBodyBytes.Length;
            remaining -= initialBodyBytes.Length;
        }

        while (remaining > 0)
        {
            var chunkSize = (int)Math.Min(readBuffer.Length, remaining);
            int read;
            try
            {
                read = await ReadFromTransportAsync(socket, sslStream, tlsHandler, readBuffer.AsMemory(0, chunkSize));
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                _config.Debug.WARNING("Closing connection: body receive timeout");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Body receive timeout");
                return null;
            }
            catch (IOException ex) when (ex.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
            {
                _config.Debug.WARNING("Closing connection: body receive timeout");
                await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Body receive timeout");
                return null;
            }
            catch (Exception ex) when (IsExpectedDisconnect(ex))
            {
                CloseTransport(socket, sslStream);
                return null;
            }

            if (read <= 0)
            {
                CloseTransport(socket, sslStream);
                return null;
            }

            Buffer.BlockCopy(readBuffer, 0, bodyBytes, offset, read);
            offset += read;
            remaining -= read;
        }

        return bodyBytes;
    }

    private async Task<string?> BufferRequestBodyToTempFileAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler,
        RequestHeadInfo headInfo,
        byte[] bufferedBodyBytes)
    {
        var tempRoot = Path.GetFullPath(_config.Upload.TempPath);
        Directory.CreateDirectory(tempRoot);
        var tempBodyPath = Path.Combine(tempRoot, $"{Guid.NewGuid():N}.upload");

        SetTransportTimeouts(socket, sslStream, _config.Upload.TimeoutSeconds * 1000, _config.Http.KeepAliveTimeoutSeconds * 1000);

        var clientIp = socket.RemoteEndPoint is IPEndPoint endpoint ? endpoint.Address.ToString() : "unknown";
        var startedAt = Stopwatch.StartNew();
        _config.Debug.INFO($"[UPLOAD][START] client={clientIp} size={headInfo.ContentLength} mime=\"{headInfo.ContentType}\"");

        long remaining = headInfo.ContentLength;
        long received = 0;
        long nextProgressThreshold = 64L * 1024L * 1024L;
        var readBuffer = new byte[_config.Http.ReadBufferSizeBytes];

        try
        {
            await using var fileStream = new FileStream(
                tempBodyPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var initialBodyBytes = bufferedBodyBytes.Length > remaining
                ? bufferedBodyBytes[..(int)remaining]
                : bufferedBodyBytes;

            if (initialBodyBytes.Length > 0)
            {
                await fileStream.WriteAsync(initialBodyBytes);
                received += initialBodyBytes.Length;
                remaining -= initialBodyBytes.Length;
            }

            while (remaining > 0)
            {
                var chunkSize = (int)Math.Min(readBuffer.Length, remaining);
                int read;
                try
                {
                    read = await ReadFromTransportAsync(socket, sslStream, tlsHandler, readBuffer.AsMemory(0, chunkSize));
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                {
                    _config.Debug.WARNING($"[UPLOAD][ERROR] client={clientIp} reason=timeout");
                    await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Upload timeout");
                    return null;
                }
                catch (IOException ex) when (ex.InnerException is SocketException sockEx && sockEx.SocketErrorCode == SocketError.TimedOut)
                {
                    _config.Debug.WARNING($"[UPLOAD][ERROR] client={clientIp} reason=timeout");
                    await SendSimpleResponseAsync(socket, sslStream, tlsHandler, HttpCodes.REQUEST_TIMEOUT, "Upload timeout");
                    return null;
                }
                catch (Exception ex) when (IsExpectedDisconnect(ex))
                {
                    _config.Debug.WARNING($"[UPLOAD][ERROR] client={clientIp} reason=client_disconnect");
                    return null;
                }

                if (read <= 0)
                {
                    _config.Debug.WARNING($"[UPLOAD][ERROR] client={clientIp} reason=client_disconnect");
                    return null;
                }

                await fileStream.WriteAsync(readBuffer.AsMemory(0, read));
                received += read;
                remaining -= read;

                if (received >= nextProgressThreshold || remaining == 0)
                {
                    _config.Debug.INFO($"[UPLOAD][PROGRESS] client={clientIp} received={received} remaining={remaining}");
                    nextProgressThreshold += 64L * 1024L * 1024L;
                }
            }

            await fileStream.FlushAsync();
        }
        catch
        {
            TryDeleteFile(tempBodyPath);
            throw;
        }

        _config.Debug.INFO($"[UPLOAD][DONE] client={clientIp} size={received} durationMs={startedAt.ElapsedMilliseconds}");
        return tempBodyPath;
    }

    private async Task SendSimpleResponseAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler, int statusCode, string message)
    {
        try
        {
            var payload = Encoding.UTF8.GetBytes(message);
            var response = Encoding.UTF8.GetBytes(
                $"HTTP/1.1 {statusCode} {HttpUtils.StatusCodeAsString(statusCode)}\r\n" +
                $"Content-Type: text/plain; charset=utf-8\r\n" +
                $"Content-Length: {payload.Length}\r\n" +
                "Connection: Close\r\n\r\n");

            await WriteToTransportAsync(socket, sslStream, tlsHandler, response);
            if (payload.Length > 0)
            {
                await WriteToTransportAsync(socket, sslStream, tlsHandler, payload);
            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            CloseTransport(socket, sslStream);
        }
    }

    private static async Task<int> ReadFromTransportAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler, byte[] buffer)
    {
        return await ReadFromTransportAsync(socket, sslStream, tlsHandler, buffer.AsMemory());
    }

    private static async Task<int> ReadFromTransportAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler, Memory<byte> buffer)
    {
        if (tlsHandler != null)
        {
            var tempBuffer = new byte[buffer.Length];
            var read = await Task.Run(() => tlsHandler.Read(tempBuffer, 0, tempBuffer.Length));
            if (read > 0)
            {
                tempBuffer.AsMemory(0, read).CopyTo(buffer);
            }

            return read;
        }

        if (sslStream != null)
        {
            return await sslStream.ReadAsync(buffer);
        }

        return await socket.ReceiveAsync(buffer, SocketFlags.None);
    }

    private static async Task WriteToTransportAsync(Socket socket, SslStream? sslStream, Tls12Handler? tlsHandler, byte[] buffer)
    {
        if (tlsHandler != null)
        {
            await Task.Run(() => tlsHandler.Write(buffer, 0, buffer.Length));
            return;
        }

        if (sslStream != null)
        {
            await sslStream.WriteAsync(buffer);
            await sslStream.FlushAsync();
            return;
        }

        var sent = 0;
        while (sent < buffer.Length)
        {
            sent += await socket.SendAsync(buffer.AsMemory(sent), SocketFlags.None);
        }
    }

    private static void SetTransportTimeouts(Socket socket, SslStream? sslStream, int receiveTimeoutMilliseconds, int sendTimeoutMilliseconds)
    {
        socket.ReceiveTimeout = receiveTimeoutMilliseconds;
        socket.SendTimeout = sendTimeoutMilliseconds;

        if (sslStream != null)
        {
            sslStream.ReadTimeout = receiveTimeoutMilliseconds;
            sslStream.WriteTimeout = sendTimeoutMilliseconds;
        }
    }

    private static void CloseTransport(Socket socket, SslStream? sslStream)
    {
        try
        {
            sslStream?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch
        {
            // ignored
        }

        try
        {
            socket.Close();
        }
        catch
        {
            // ignored
        }
    }

    private static bool IsExpectedDisconnect(Exception exception)
    {
        return exception switch
        {
            ObjectDisposedException => true,
            IOException { InnerException: SocketException inner } => IsExpectedSocketError(inner.SocketErrorCode),
            SocketException socketException => IsExpectedSocketError(socketException.SocketErrorCode),
            _ => false
        };
    }

    private static bool IsExpectedSocketError(SocketError socketError)
    {
        return socketError is
            SocketError.ConnectionAborted or
            SocketError.ConnectionReset or
            SocketError.Shutdown or
            SocketError.NotConnected or
            SocketError.OperationAborted or
            SocketError.TimedOut;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignored
        }
    }

    private sealed class RequestEnvelope(byte[] headerBytes, byte[] bodyBytes, string? bodyTempFilePath, SemaphoreLease? uploadLease) : IDisposable
    {
        public byte[] HeaderBytes { get; } = headerBytes;
        public byte[] BodyBytes { get; } = bodyBytes;
        public string? BodyTempFilePath { get; } = bodyTempFilePath;
        private SemaphoreLease? UploadLease { get; } = uploadLease;

        public void Dispose()
        {
            UploadLease?.Dispose();
        }
    }

    private sealed record RequestHeadInfo(long ContentLength, string ContentType, bool IsMultipartUpload, bool HasChunkedTransferEncoding)
    {
        public static RequestHeadInfo Empty { get; } = new(0, string.Empty, false, false);
    }

    private sealed record HeaderReadResult(byte[] HeaderBytes, byte[] BufferedBodyBytes);

    private sealed class SemaphoreLease(SemaphoreSlim semaphore) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }

            semaphore.Release();
        }
    }

    private object? GetInstance(Request req)
    {
        var candidateControllers = routes.Where(map => req.Url.StartsWith(map.Path)).ToArray();

        foreach (var map in candidateControllers)
        {
            if (map.SubRoutes.Count == 0)
            {
                continue;
            }

            //slice relative path, for example if the map path is "/api" and the request url is "/api/status", the relative path will be "/status"
            var relativePath = req.Url[map.Path.Length..];
            var candidateMethods = map.SubRoutes.Where(r => r.HttpMethod == req.Method).ToList();

            //if the root is called, activate the first "/" subRoute if exists, else return 404
            if (relativePath == "")
            {
                RoutableMethod? rootRoute = candidateMethods.Find(sr => sr.Path == "/");
                if (!rootRoute.HasValue) return null;
                //inject Request and Response in the class if there are any parameter with those types, this allows to avoid having to declare them in the route method

                return (map.Class, rootRoute.Value); //activation is done in replacement of the Process() function call
            }

            foreach (var route in candidateMethods)
            {
                //get public instance fields

                if (route.Path == relativePath)
                    return (map.Class, route);

                var pattern = "^" + Regex.Replace(route.Path, @":[^/]+", @"[^/]+") + "$";

                if (!Regex.IsMatch(relativePath, pattern)) continue;
                //we extract the parameters from the url and add them to the request parameters
                var routeParts = route.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var relativeParts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < routeParts.Length; i++)
                {
                    if (!routeParts[i].StartsWith(':')) continue;
                    var paramName = routeParts[i][1..];
                    var paramValue = relativeParts[i];
                    req.Parameters[paramName] = paramValue;
                }

                return (map.Class, route);
            }
        }

        return null;
    }


    private async Task ProcessRequestAsync(Request req, Response res)
    {
        try
        {
            //check if request is valid                    
            if (!req.ValidRequest)
            {
                _config.Debug.WARNING($"{req.Method} '{req.Url}' {HttpCodes.NOT_FOUND} (Invalid Request)");
                new Error(res, _config, "Invalid Request", HttpCodes.NOT_FOUND).Throw();
                return;
            }


            if (!ExecuteModule(ModuleType.RequestInterceptor, _config, req, res))
            {
                return;
            }

            /*
            //check if server is launched with --listFiles
            if (_config.GetRawArguments().Contains("--listFiles"))
            {
                if (PathUtils.SafeRequestOrBan(_config, req, res)) return;

                new FileList(req, res, _config).Get();
                return;
            }
            */


            //if global CORS are set in configuration, check if the request is allowed
            if (_config.GlobalCors != null)
            {
                if (!_config.GlobalCors.IsRequestAllowed(req))
                {
                    _config.Debug.WARNING($"{req.Method} '{req.Url}' {HttpCodes.FORBIDDEN} (CORS not allowed)");
                    new Error(res, _config, "CORS not allowed", HttpCodes.FORBIDDEN).Throw();
                    return;
                }
            }

            //if dev has used the express mapping, we run the mapped function
            //if (RunIfExpressMapping(req, res)) return;

            if (req.IsWebSocket())
            {
                var endpoint = _config.WebSocketRouter.Match(req.Url);
                if (endpoint == null)
                {
                    _config.Debug.WARNING($"WebSocket '{req.Url}' {HttpCodes.NOT_FOUND} (Route not found)");
                    new Error(res, _config, "WebSocket route not found", HttpCodes.NOT_FOUND).Throw();
                    return;
                }

                var connection = new WebSocketConnection(req, res, _config, endpoint);

                try
                {
                    if (_config.WebSocketRouter.ConnectionCount >= _config.WebSocketOptions.MaxConnectionsTotal)
                    {
                        _config.Debug.WARNING($"WebSocket '{req.Url}' {HttpCodes.SERVICE_UNAVAILABLE} (Global connection limit reached)");
                        new Error(res, _config, "WebSocket global connection limit reached", HttpCodes.SERVICE_UNAVAILABLE).Throw();
                        return;
                    }

                    if (endpoint.ConnectionCount >= _config.WebSocketOptions.MaxConnectionsPerEndpoint)
                    {
                        _config.Debug.WARNING($"WebSocket '{req.Url}' {HttpCodes.TOO_MANY_REQUESTS} (Endpoint connection limit reached)");
                        new Error(res, _config, "WebSocket endpoint connection limit reached", HttpCodes.TOO_MANY_REQUESTS).Throw();
                        return;
                    }

                    endpoint.Add(connection);
                    _config.Debug.INFO($"[WS][CONNECT] id={connection.Id} ip={connection.RemoteIp} path={connection.Path}");
                    await connection.Runtime.ProcessAsync(() => endpoint.ConfigureAsync(connection));
                }
                finally
                {
                    var duration = DateTime.UtcNow - connection.ConnectedAtUtc;
                    _config.Debug.INFO($"[WS][DISCONNECT] id={connection.Id} ip={connection.RemoteIp} path={connection.Path} durationMs={duration.TotalMilliseconds:0}");
                    endpoint.Remove(connection);
                }

                return;
            }

            //We check if the route requested is handled by any servlet
            var o = GetInstance(req);

            if (o != null)
            {
                ParameterInfo[] parameters;
                switch (o)
                {
                    case (null, RoutableMethod route):
                        if (route.Type != RoutableMethodType.Delegate)
                        {
                            throw new Exception("Invalid route type, expected delegate");
                        }

                        parameters = route.Delegate!.GetMethodInfo().GetParameters();
                        List<object> callingParams = [];

                        foreach (var field in parameters)
                        {
                            if (field.ParameterType == typeof(Request))
                            {
                                callingParams.Add(req);
                            }
                            else if (field.ParameterType == typeof(Response))
                            {
                                callingParams.Add(res);
                            }
                        }


                        if (!ExecuteModule(ModuleType.RequestHandlerInterceptor, _config, req, res,
                                route.Delegate!.Method))
                        {
                            return;
                        }

                        route.Delegate!.DynamicInvoke(callingParams.ToArray());
                        return;
                    case (Type tipo, RoutableMethod route):
                        if (route.Type != RoutableMethodType.Method)
                        {
                            throw new Exception("Invalid route type, expected Class and Method");
                        }

                        parameters = route.MethodInfo!.GetParameters();
                        var instance = Activator.CreateInstance(tipo);

                        //get public instance fields
                        var fields = tipo
                            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(fi => fi.FieldType == typeof(Request) || fi.FieldType == typeof(Response));
                        foreach (var field in fields)
                        {
                            if (field.FieldType == typeof(Request))
                            {
                                field.SetValue(instance, req);
                            }
                            else if (field.FieldType == typeof(Response))
                            {
                                field.SetValue(instance,
                                    res); //response is not available at this point, we will set it to null and then inject the real response in the Process() function
                            }
                        }

                        _config.Debug.INFO($"HTTP Request | {req.Method} {req.Url}");
                        //try automatically inject parameters from request to the method
                        //it must use decorator to specify the parameters to inject, for example [FromQuery] or [FromBody]
                        //try collect parameters
                        var methodParameters = route.MethodInfo.GetParameters()
                            .Where(p => p.GetCustomAttribute<NamedParameter>() != null);

                        List<object> injectionParameters = [];

                        foreach (var methodParameter in methodParameters)
                        {
                            var paramAttributes = methodParameter.GetCustomAttribute<NamedParameter>();

                            if (paramAttributes == null) break;
                            if (paramAttributes.Body)
                            {
                                var deserializedBody = JsonSerializer.Deserialize<Dictionary<string, object>>(req.Body);
                                if (deserializedBody == null || !deserializedBody.ContainsKey(paramAttributes.Name))
                                {
                                    new Error(res,
                                            _config,
                                            $"Missing value for parameter {paramAttributes!.Name}",
                                            HttpCodes.BAD_REQUEST)
                                        .Throw();
                                    return;
                                }

                                //parameters must be collected in order to be applied
                                var paramValue = deserializedBody[paramAttributes.Name] as string;
                                var parsedType = TypeUtils.ConvertToType(paramValue!, methodParameter.ParameterType);
                                injectionParameters.Add(parsedType);
                            }
                            else
                            {
                                if ((!req.Parameters.ContainsKey(paramAttributes.Name) ||
                                     req.Parameters[paramAttributes.Name] == "") && paramAttributes.Required)
                                {
                                    new Error(res,
                                            _config,
                                            $"Missing value for parameter {paramAttributes!.Name}",
                                            HttpCodes.BAD_REQUEST)
                                        .Throw();
                                    return;
                                }

                                //parameters must be collected in order to be applied
                                var paramValue = req.Parameters[paramAttributes!.Name];
                                var parsedType = TypeUtils.ConvertToType(paramValue, methodParameter.ParameterType);
                                injectionParameters.Add(parsedType);
                            }
                        }

                        if (parameters.Length != methodParameters.Count())
                        {
                            //method has parameters that are not noted by decorators, we will try to inject req and res if those are needed
                            foreach (var parameter in parameters)
                            {
                                if (parameter.ParameterType == typeof(Request))
                                {
                                    injectionParameters.Add(req);
                                }
                                else if (parameter.ParameterType == typeof(Response))
                                {
                                    injectionParameters.Add(res);
                                }
                                else if (parameter.ParameterType == typeof(Configuration))
                                {
                                    injectionParameters.Add(_config);
                                }
                                else
                                {
                                    injectionParameters.Add(new object()); //fall back
                                }
                            }
                        }

                        if (!ExecuteModule(ModuleType.RequestHandlerInterceptor, _config, req, res, route.MethodInfo))
                        {
                            return;
                        }

                        switch (parameters.Length)
                        {
                            case 0:
                                route.MethodInfo.Invoke(instance, null);
                                break;
                            default:
                                route.MethodInfo.Invoke(instance, injectionParameters.ToArray());
                                break;
                        }

                        return;
                    default:
                        Console.WriteLine(o.GetType());
                        throw new Exception($"Developer tried to map an invalid object to a route -> {o.GetType()}");
                }
            }

            //the client searched for a route that is not mapped by any servlet
            //so we do some other checks like root page or static resource
            //if no root page is set we search for and index.html file, else we show the default home page
            if (req.Url == "/")
            {
                //if the client is requesting the root file, we check if there is an index.html file
                //if not, we use the default servlet
                if (File.Exists(_config.StaticFolderPath + "/index.html"))
                {
                    _config.Debug.INFO($"{req.Method} '{req.Url}' 200");
                    res.SendHtmlFile(_config.StaticFolderPath + "/index.html");
                }
                else
                {
                    _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Default Index Page)");
                    new Index(res, _config).Get();
                }
            }
            else
            {
                //we check if the client is requesting a resource, else 404 not found
                //to check if the path is safe we use the same regex used in send.js
                //see: https://github.com/pillarjs/send/blob/master/index.js#L63
                if (PathUtils.SafeRequestOrBan(_config, req, res))
                {
                    return;
                }

                //if the path is safe, the static folder is set and the file exists, we send it
                if (_config.StaticFolderPath != "" && File.Exists(_config.StaticFolderPath + "/" + req.Url))
                {
                    //config.debug.INFO($"Static file found, serving '{req.URL}'");
                    _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Static file)");
                    res.SendFile(_config.StaticFolderPath + "/" + req.Url);
                }
                else if (_config.ServeEmbeddedResource &&
                         ResourceUtils.IsEmbeddedResource(req.Url, _config.EmbeddedResourcePrefix))
                {
                    _config.Debug.INFO($"{req.Method} '{req.Url}' 200 (Embedded resource)");
                    var resource = ResourceUtils.LoadResource<object>(req.Url, _config.EmbeddedResourcePrefix) ??
                                   throw new Exception("Resource not found");
                    res.SendObject(resource, req.Url);
                }
                else
                {
                    //if no servlet or static file found, send 404
                    _config.Debug.INFO($"{req.Method} '{req.Url}' 404 (Resource not found)");
                    new Error(res, _config, "Page not found", HttpCodes.NOT_FOUND).Throw();
                }
            }
        }
        catch (Exception e)
        {
            //config.debug.ERROR("Error handling request ->\n " + e);
            _config.Debug.ERROR($"{req.Method} '{req.Url}' 500 (Internal Server Error)\n{e}");
            //we show an error page with the message and code 500
            new Error(res, _config, e.ToString(), HttpCodes.INTERNAL_SERVER_ERROR).Throw();
        }
    }


    private void MapRoutes()
    {
        //order : ExpressMapping -> Controllers -> Servlets -> Static files
        //Servlets must be deprecated

        _config.Debug.INFO("Collecting routes...");

        //express routes are now treated equally to controller routes
        routes.AddRange(_config.ExpressRoutes.Select(r => new Map()
        {
            Path = r.Path,
            SubRoutes =
            [
                new RoutableMethod()
                {
                    Delegate = r.Delegate,
                    HttpMethod = r.HttpMethod,
                    Path = "/"
                }
            ]
        }));


        var classes = ClassUtils.GetClassesWithAttribute<Controller>();


        foreach (var c in classes)
        {
            //get only class with the attribute [Controller]

            var attr = c.GetCustomAttribute<Controller>(false);
            if (attr == null) continue;
            _config.Debug.INFO($"Controller | {c.Name} -> {attr.Path}");

            var map = new Map()
            {
                Path = attr.Path,
                Class = c,
                SubRoutes = []
            };

            var methods = c.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<Route>(false) != null);
            foreach (var m in methods)
            {
                var routeAttr = m.GetCustomAttribute<Route>(true);
                if (routeAttr == null) continue;
                var logStr =
                    $"ROUTE | {Terminal.FG_TO_STRING(FgColor.Green)}{routeAttr.Method}{Terminal.RESET} -> {Terminal.FG_TO_STRING(FgColor.Yellow)}{attr.Path}{routeAttr.Path}{Terminal.RESET} {m.Name}";
                _config.Debug.INFO(logStr);

                map.SubRoutes.Add(new RoutableMethod()
                {
                    Path = routeAttr.Path,
                    HttpMethod = routeAttr.Method,
                    MethodInfo = m
                });
            }

            var webSocketMethods = c.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<Ws>(false) != null);

            foreach (var m in webSocketMethods)
            {
                var wsAttr = m.GetCustomAttribute<Ws>(true);
                if (wsAttr == null) continue;

                var fullPath = WebSocketRouter.CombinePaths(attr.Path, wsAttr.Path);
                _config.WebSocket(fullPath, socket =>
                {
                    var instance = Activator.CreateInstance(c);
                    if (instance == null)
                    {
                        throw new Exception($"Cannot create controller instance for {c.FullName}");
                    }

                    var fields = c
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(fi => fi.FieldType == typeof(Request) || fi.FieldType == typeof(Response));

                    foreach (var field in fields)
                    {
                        if (field.FieldType == typeof(Request))
                        {
                            field.SetValue(instance, socket.Request);
                        }
                        else if (field.FieldType == typeof(Response))
                        {
                            field.SetValue(instance, socket.Response);
                        }
                    }

                    var methodParameters = m.GetParameters();
                    var callingParams = methodParameters.Select<ParameterInfo, object>(parameter =>
                    {
                        if (parameter.ParameterType == typeof(WebSocketConnection))
                        {
                            return socket;
                        }

                        if (parameter.ParameterType == typeof(Request))
                        {
                            return socket.Request;
                        }

                        if (parameter.ParameterType == typeof(Response))
                        {
                            return socket.Response;
                        }

                        if (parameter.ParameterType == typeof(Configuration))
                        {
                            return _config;
                        }

                        throw new Exception(
                            $"Unsupported WebSocket controller parameter {parameter.Name} on {c.Name}.{m.Name}");
                    }).ToArray();

                    var result = m.Invoke(instance, callingParams);
                    return result is Task task ? task : Task.CompletedTask;
                });
            }

            routes.Add(map);
        }
    }


    public List<Map> GetRoutes()
    {
        return routes;
    }

    public Configuration GetConfiguration()
    {
        return _config;
    }
}
