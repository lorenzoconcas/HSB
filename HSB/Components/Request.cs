using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using HSB.Components;
using HSB.Constants;
using HSB.Utils;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

public class Request : IDisposable
{
    private static readonly HashSet<string> NonRepeatableHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Content-Length",
        "Connection",
        "Upgrade",
        "Sec-WebSocket-Key",
        "Sec-WebSocket-Version"
    };

    //support-variables
    readonly string reqText = "";
    readonly List<string> requestContent;
    internal Socket connectionSocket;
    internal byte[] rawData;
    internal byte[] rawBody;
    private readonly string? rawBodyFilePath;
    private readonly Configuration config;


    //Request variables
    public bool ValidRequest;
    private string body = "";
    readonly Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> parameters = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> rawHeaders = [];
    readonly Dictionary<string, Cookie> cookies = new(StringComparer.OrdinalIgnoreCase);
    readonly List<Cookie> responseCookies = [];
    public readonly bool IsTls;

    //Auth structs
    private Tuple<string, string>? basicAuth;
    private OAuth10Information? oAuth10Information;
    private string oAuth20Token = "";
    private Session session = new();
    private MultiPartFormData? multiPartFormData;
    private Form? form;

    public bool IsValidRequest = true;
    public int InvalidStatusCode { get; private set; } = HttpCodes.BAD_REQUEST;
    public string InvalidReason { get; private set; } = "Invalid Request";

    public Request(byte[] data, Socket socket, Configuration config, bool isTls = false)
        : this(data, [], null, socket, config, isTls)
    {
    }

    internal Request(
        byte[] headerData,
        byte[] bodyData,
        string? bodyFilePath,
        Socket socket,
        Configuration config,
        bool isTls = false)
    {
        connectionSocket = socket;
        rawData = headerData;
        rawBody = bodyData;
        rawBodyFilePath = bodyFilePath;
        this.config = config;
        requestContent = [];
        IsTls = isTls;

        if (headerData.Length == 0)
        {
            return;
        }
        var rEP = socket.RemoteEndPoint;
        //extract ipv4 or ipv6 from the remote endpoint
        if (rEP != null)
        {
            var rIEP = (IPEndPoint)rEP;
            ClientIp = rIEP.Address.ToString();
            ClientPort = rIEP.Port;
            ClientIpVersion = rIEP.AddressFamily;
        }

        switch (EncodingUtils.GetEncoding(headerData))
        {
            case UTF8Encoding:
                reqText = Encoding.UTF8.GetString(headerData);
                break;
            case UTF32Encoding:
                reqText = Encoding.UTF32.GetString(headerData);
                break;
            case ASCIIEncoding:
                reqText = Encoding.ASCII.GetString(headerData);
                break;
        }

        if (reqText.Replace("\0", "") == "")
        {
            //note:
            //it can happen in programs like postman that a request to localhost produces two requests
            //one for IPv6 and one for IPv4
            //I don't know why but the second request is invalid
            ValidRequest = false;
            config.Debug.INFO("Got an invalid request, ignoring...");
            requestContent.Add(" ");
            return;
        }
        // reqText = Encoding.UTF8.GetString(data);
        requestContent = [.. reqText.Split("\r\n", StringSplitOptions.None)];
        ParseRequest();


    }

    private void ParseRequest()
    {
        if (string.IsNullOrWhiteSpace(reqText))
        {
            //empty request
            Url = "/";
            Protocol = HttpProtocol.HTTP1_0;
            Method = HttpMethod.Get;
            body = "";
            rawBody = [];
            session = new Session(); //default, invalid session
            Terminal.Info("Got an empty request, setting default values");
            return;
        }

        try
        {
            if (requestContent.Count == 0 || string.IsNullOrWhiteSpace(requestContent[0]))
            {
                MarkInvalidRequest("Missing request line");
                return;
            }

            string[] firstLine = requestContent[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (firstLine.Length != 3)
            {
                MarkInvalidRequest($"Malformed request line: {requestContent[0]}");
                return;
            }

            Method = HttpUtils.GetMethod(firstLine[0]);

            if (Method == HttpMethod.Unknown)
            {
                MarkInvalidRequest($"Unsupported HTTP method: {firstLine[0]}");
                return;
            }

            string rawTarget = firstLine[1];

            if (string.IsNullOrWhiteSpace(rawTarget))
            {
                MarkInvalidRequest("Missing request target");
                return;
            }

            string[] targetParts = rawTarget.Split('?', 2);
            Url = targetParts[0];

            if (string.IsNullOrWhiteSpace(Url))
            {
                Url = "/";
            }

            if (!Url.StartsWith('/') && Url.EndsWith('/'))
            {
                //delete last "/" if url is like "example.com/"
                Url = Url[..^1];
            }

            Protocol = HttpUtils.GetProtocol(firstLine[2]);

            if (targetParts.Length == 2)
            {
                ParseQueryString(targetParts[1]);
            }

            ParseHeaders();
            ExtractAuthData();
            TryExtractAndSetOAuth1_0();

            //oAuth2.0 token 
            if (parameters.TryGetValue("access_token", out string? tkn))
            {
                oAuth20Token = tkn;
            }

            ParseCookies();
            ResolveSession();
            AttachBodyData();

            if (IsFileUpload() && headers.TryGetValue("Content-Type", out string? contentType))
            {
                string[] contentTypeParts = contentType.Split("boundary=", 2, StringSplitOptions.None);

                if (contentTypeParts.Length == 2 && !string.IsNullOrWhiteSpace(contentTypeParts[1]))
                {
                    multiPartFormData = rawBodyFilePath != null
                        ? new MultiPartFormData(rawBodyFilePath, contentTypeParts[1], config.Upload, config.Http)
                        : new MultiPartFormData(rawBody, contentTypeParts[1], config.Upload, config.Http);
                }
                else
                {
                    MarkInvalidRequest("Missing multipart boundary", HttpCodes.BAD_REQUEST);
                    return;
                }
            }

            if (IsFormUpload())
            {
                form = new Form(body);
            }

            ValidRequest = true;
        }
        catch (MultipartParseException e)
        {
            MarkInvalidRequest(e.Message, e.StatusCode);
        }
        catch (Exception e)
        {
            MarkInvalidRequest(e.Message);
        }
    }

    private void MarkInvalidRequest(string reason, int statusCode = HttpCodes.BAD_REQUEST)
    {
        config.Debug.WARNING($"Invalid request: {reason}");
        ValidRequest = false;
        IsValidRequest = false;
        InvalidStatusCode = statusCode;
        InvalidReason = string.IsNullOrWhiteSpace(reason) ? "Invalid Request" : reason;
    }

    private void ParseQueryString(string queryString)
    {
        if (string.IsNullOrWhiteSpace(queryString))
        {
            return;
        }

        string[] requestParameters = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in requestParameters)
        {
            if (p.Length > 2048)
            {
                continue;
            }
            string[] pair = p.Split('=', 2);
            string key = SafeUrlDecode(pair[0]);
            string value = pair.Length == 2 ? SafeUrlDecode(pair[1]) : string.Empty;

            if (!string.IsNullOrWhiteSpace(key))
            {
                parameters[key] = value;
            }
        }
    }

    private void ParseHeaders()
    {
        for (int i = 1; i < requestContent.Count; i++)
        {
            string r = requestContent[i];

            if (r == "")
            {
                break;
            }

            if (r.Length > 4096)
            {
                continue;
            }
            rawHeaders.Add(r);

            int separatorIndex = r.IndexOf(':');

            if (separatorIndex <= 0)
            {
                throw new InvalidDataException($"Malformed header: {r}");
            }

            string key = r[..separatorIndex].Trim();
            string value = r[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key) || !IsValidHeaderName(key))
            {
                throw new InvalidDataException($"Invalid header name: {key}");
            }

            if (value.Contains('\r') || value.Contains('\n') || value.Contains('\0'))
            {
                throw new InvalidDataException($"Invalid header value for {key}");
            }

            if (!headers.TryAdd(key, value))
            {
                if (NonRepeatableHeaders.Contains(key))
                {
                    throw new InvalidDataException($"Duplicate header not allowed: {key}");
                }

                headers[key] = CombineHeaderValues(key, headers[key], value);
            }
        }
    }

    private void ExtractAuthData()
    {
        if (!headers.TryGetValue("Authorization", out string? value) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            string base64Credentials = value[6..].Trim();

            try
            {
                string decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
                string[] credentials = decodedCredentials.Split(':', 2);

                if (credentials.Length == 2)
                {
                    basicAuth = new Tuple<string, string>(credentials[0], credentials[1]);
                }
            }
            catch
            {
                // Invalid basic auth payload. Ignore it and keep request parsing alive.
            }

            return;
        }

        if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            oAuth20Token = value[7..].Trim();
        }
    }

    private void ParseCookies()
    {
        if (!headers.TryGetValue("Cookie", out string? val) || string.IsNullOrWhiteSpace(val))
        {
            return;
        }

        var cookieValues = val.Split(';', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawCookie in cookieValues)
        {
            if (rawCookie.Length > 4096)
            {
                continue;
            }
            string cookieText = rawCookie.Trim();

            if (string.IsNullOrWhiteSpace(cookieText) || !cookieText.Contains('='))
            {
                continue;
            }

            string[] pair = cookieText.Split('=', 2);
            string cookieName = pair[0].Trim();

            if (string.IsNullOrWhiteSpace(cookieName))
            {
                continue;
            }

            try
            {
                cookies[cookieName] = new Cookie(cookieText);
            }
            catch
            {
                // Invalid cookie format. Ignore single malformed cookie.
            }
        }
    }

    private void ResolveSession()
    {
        if (cookies.TryGetValue("hsbst", out Cookie? cookie) && SessionManager.GetInstance().IsValidSession(cookie.value))
        {
            session = SessionManager.GetInstance().GetSession(cookie.value);
            return;
        }

        session = new Session()
        {
            ExpirationTime = DateTime.Now.AddTicks((long)config.DefaultSessionExpirationTime).Ticks
        };

        var sessionToken = SessionManager.GetInstance().CreateSession(session);

        Cookie c = new()
        {
            name = "hsbst",
            value = sessionToken,
            expiration = DateTime.Now.AddTicks((long)config.DefaultSessionExpirationTime),
            path = "/",
            priority = Cookie.CookiePriority.HIGH
        };

        responseCookies.Add(c);
    }

    private void AttachBodyData()
    {
        if (rawBodyFilePath != null)
        {
            rawBody = [];
            body = string.Empty;
            return;
        }

        if (rawBody.Length == 0)
        {
            body = string.Empty;
            return;
        }

        body = Encoding.UTF8.GetString(rawBody);
    }

    private static string SafeUrlDecode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            return Uri.UnescapeDataString(value.Replace("+", " "));
        }
        catch
        {
            return value;
        }
    }

    /// <summary>
    /// Return the method of the request
    /// </summary>
    public HttpMethod Method { get; private set; } = HttpMethod.Unknown;

    /// <summary>
    /// Return the protocol of the request
    /// </summary>
    public HttpProtocol Protocol  { get; private set; } = HttpProtocol.UNKNOWN;

    /// <summary>
    /// Return the url of the request
    /// </summary>
    public string Url { get; private set; } = "";

    /// <summary>
    /// Return the ip of the client (request source ip)
    /// </summary>
    public string ClientIp { get; } = "";

    /// <summary>
    /// Return the port of the client (request source port)
    /// </summary>
    public int ClientPort { get; } = -1;

    /// <summary>
    /// Return the ip version of the client (request source ip version (v4 or v6))
    /// </summary>
    public AddressFamily ClientIpVersion { get; }

    /// <summary>
    /// Return the raw body of the request
    /// </summary>
    public byte[] RawBody => rawBody;
    /// <summary>
    /// Return the body of the request parsed as string
    /// </summary>
    public string Body => body;
    /// <summary>
    /// Return the headers
    /// </summary>
    public Dictionary<string, string> Headers => headers;
    /// <summary>
    /// Return the unparsed headers
    /// </summary>
    public List<string> RawHeaders => rawHeaders;
    /// <summary>
    /// Return the parameters
    /// </summary>
    public Dictionary<string, string> Parameters => parameters;
    /// <summary>
    /// Return the session associated with the request
    /// </summary>
    /// <returns></returns>
    public Session GetSession() => session;
    public IReadOnlyList<Cookie> ResponseCookies => responseCookies;
    public Tuple<string, string>? GetBasicAuthInformation() => basicAuth;
    public OAuth10Information? GetOAuth1_0Information() => oAuth10Information;


    /// <summary>
    /// Test if a request contains a JSON document in the body
    /// </summary>
    /// <returns></returns>
    public bool IsJson() => headers.TryGetValue("Content-Type", out string? contentType) && contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// Returns if the request is an ajax request
    /// </summary>
    public bool IsAjaxRequest => headers.ContainsKey("X-Requested-With") && headers["X-Requested-With"] == "XMLHttpRequest";
    /// <summary>
    /// Returns true if the request is a websocket request
    /// </summary>
    /// <returns></returns>
    public bool IsWebSocket()
    {
        return
            headers.TryGetValue("Connection", out string? connection) && connection.Contains("upgrade", StringComparison.OrdinalIgnoreCase) &&
            headers.TryGetValue("Upgrade", out string? upgrade) && upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Returns true if the request is a file upload
    /// </summary>
    /// <returns></returns>
    public bool IsFileUpload() => headers.TryGetValue("Content-Type", out string? contentType) && contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// Returns true if the request is a form upload
    /// </summary>
    /// <returns></returns>
    public bool IsFormUpload() => headers.TryGetValue("Content-Type", out string? contentType) && contentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    /// <summary>
    /// Returns the form data if the request is a multipart form data upload, else null
    /// </summary>
    /// <returns></returns>
    public MultiPartFormData? GetMultiPartFormData() => multiPartFormData;
    /// <summary>
    /// Returns the form data if the request is a form upload, else null
    /// </summary>
    /// <returns></returns>
    public Form? GetFormData() => form;

    public void Dispose()
    {
        multiPartFormData?.Dispose();

        if (rawBodyFilePath == null)
        {
            return;
        }

        try
        {
            if (File.Exists(rawBodyFilePath))
            {
                File.Delete(rawBodyFilePath);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    //Debug functions

    public Socket GetSocket() => connectionSocket;

    public string GetRawRequestText => reqText;
    public void DumpRequest(string path = "./request.txt")
    {
        File.WriteAllBytes(path, rawData);
    }
    public void DumpBody(string path = "./body.txt")
    {
        File.WriteAllText(path, body);
    }
    internal string GetRawRequest => reqText;
    internal string RawMethod => requestContent.Count == 0
        ? string.Empty
        : requestContent[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

    //utilities functions
    public void FullPrint()
    {
        Terminal.Debug("PRINTING RAW REQUEST\n====================");
        Terminal.Info(reqText);
        Terminal.Debug("\n====================");
        Terminal.Info($"Has basic auth? {basicAuth != null}");
        if (basicAuth != null)
            Terminal.Info(basicAuth);
        Terminal.Info($"Has oauth1.0? {oAuth10Information != null}");
        if (oAuth10Information != null)
            Terminal.Info(oAuth10Information);

        Terminal.Info($"Has oAuth2.0? {oAuth20Token != ""} {oAuth20Token}");

    }
    private void TryExtractAndSetOAuth1_0()
    {
        OAuth10Information data = new(parameters);
        if (data.IsValid())
            oAuth10Information = data;

    }

    public override string ToString()
    {
        string str = Method.ToString() + " - " + Url + " - " + Protocol.ToString();
        return str;
    }

    private static bool IsValidHeaderName(string key)
    {
        foreach (var ch in key)
        {
            if (char.IsControl(ch) || char.IsWhiteSpace(ch))
            {
                return false;
            }
        }

        return true;
    }

    private static string CombineHeaderValues(string key, string currentValue, string newValue)
    {
        var separator = key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ? "; " : ", ";
        return string.Concat(currentValue, separator, newValue);
    }
}

//todo -> move to a separate file, ideally in Constants
public static class HttpUtils
{
    public static string MethodAsString(HttpMethod method) => method switch
    {
        HttpMethod.Get => "GET",
        HttpMethod.Post => "POST",
        HttpMethod.Put => "PUT",
        HttpMethod.Delete => "DELETE",
        HttpMethod.Head => "HEAD",
        HttpMethod.Patch => "PATCH",
        HttpMethod.Options => "OPTIONS",
        HttpMethod.Trace => "TRACE",
        HttpMethod.Connect => "CONNECT",
        _ => "GET", //failsafe?
    };

    public static string ProtocolAsString(HttpProtocol protocol) => protocol switch
    {
        HttpProtocol.HTTP1_0 => "HTTP/1.0",
        HttpProtocol.HTTP1_1 => "HTTP/1.1",
        HttpProtocol.HTTP2_0 => "HTTP/2.0",
        HttpProtocol.HTTP3_0 => "HTTP/3.0",
        _ => "HTTP/1.0",
    };
    public static HttpMethod GetMethod(string data) => data switch
    {
        "GET" => HttpMethod.Get,
        "POST" => HttpMethod.Post,
        "PUT" => HttpMethod.Put,
        "DELETE" => HttpMethod.Delete,
        "HEAD" => HttpMethod.Head,
        "PATCH" => HttpMethod.Patch,
        "OPTIONS" => HttpMethod.Options,
        "TRACE" => HttpMethod.Trace,
        "CONNECT" => HttpMethod.Connect,
        _ => HttpMethod.Unknown
    };

    public static HttpProtocol GetProtocol(string data) => data switch
    {
        "HTTP/1.0" => HttpProtocol.HTTP1_0,
        "HTTP/1.1" => HttpProtocol.HTTP1_1,
        "HTTP/2.0" => HttpProtocol.HTTP2_0,
        "HTTP/3.0" => HttpProtocol.HTTP3_0,
        _ => HttpProtocol.UNKNOWN
    };

    public static string StatusCodeAsString(int statusCode) => statusCode switch
    {
        HttpCodes.CONTINUE => "Continue",
        HttpCodes.SWITCHING_PROTOCOLS => "Switching Protocols",
        HttpCodes.OK => "OK",
        HttpCodes.CREATED => "Created",
        HttpCodes.ACCEPTED => "Accepted",
        HttpCodes.NO_CONTENT => "No Content",
        HttpCodes.MOVED_PERMANENTLY => "Moved Permanently",
        HttpCodes.FOUND => "Found",
        HttpCodes.SEE_OTHER => "See Other",
        HttpCodes.NOT_MODIFIED => "Not Modified",
        HttpCodes.BAD_REQUEST => "Bad Request",
        HttpCodes.UNAUTHORIZED => "Unauthorized",
        HttpCodes.FORBIDDEN => "Forbidden",
        HttpCodes.NOT_FOUND => "Not Found",
        HttpCodes.METHOD_NOT_ALLOWED => "Method Not Allowed",
        HttpCodes.REQUEST_TIMEOUT => "Request Timeout",
        HttpCodes.CONFLICT => "Conflict",
        HttpCodes.LENGTH_REQUIRED => "Length Required",
        HttpCodes.PAYLOAD_TOO_LARGE => "Payload Too Large",
        HttpCodes.UNSUPPORTED_MEDIA_TYPE => "Unsupported Media Type",
        HttpCodes.TOO_MANY_REQUESTS => "Too Many Requests",
        HttpCodes.REQUEST_HEADER_FIELDS_TOO_LARGE => "Request Header Fields Too Large",
        HttpCodes.INTERNAL_SERVER_ERROR => "Internal Server Error",
        HttpCodes.NOT_IMPLEMENTED => "Not Implemented",
        HttpCodes.SERVICE_UNAVAILABLE => "Service Unavailable",
        HttpCodes.GATEWAY_TIMEOUT => "Gateway Timeout",
        _ => "Status"
    };

}
