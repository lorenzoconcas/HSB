using System.Net;

using System.Net.Sockets;
using System.Text;
using HSB.Components;
using HSB.Constants;
using HSB.Utils;
using HttpMethod = HSB.Constants.HttpMethod;

namespace HSB;

public class Request
{
    //support-variables
    readonly string reqText = "";
    readonly List<string> requestContent;
    internal Socket connectionSocket;
    internal byte[] rawData;
    internal byte[] rawBody;
    private readonly Configuration config;


    //Request variables
    public bool ValidRequest;
    private string body = "";
    readonly Dictionary<string, string> headers = [];
    readonly Dictionary<string, string> parameters = [];
    readonly List<string> rawHeaders = [];
    readonly Dictionary<string, Cookie> cookies = [];
    public readonly bool IsTls;

    //Auth structs
    private Tuple<string, string>? basicAuth;
    private OAuth10Information? oAuth10Information;
    private string oAuth20Token = "";
    private Session session = new();
    private MultiPartFormData? multiPartFormData;
    private Form? form;

    public bool IsValidRequest = true;
    public Request(byte[] data, Socket socket, Configuration config, bool isTls = false)
    {
        connectionSocket = socket;
        rawData = data;
        rawBody = [];
        this.config = config;
        requestContent = [];
        IsTls = isTls;

        if (data.Length == 0)
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

        switch (EncodingUtils.GetEncoding(data))
        {
            case UTF8Encoding:
                reqText = Encoding.UTF8.GetString(data);
                break;
            case UTF32Encoding:
                reqText = Encoding.UTF32.GetString(data);
                break;
            case ASCIIEncoding:
                reqText = Encoding.ASCII.GetString(data);
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
        requestContent = [.. reqText.Split("\r\n")];
        ParseRequest();


    }

    private void ParseRequest()
    {
        if (reqText == "")
        {
            //empty request
            Url = "/";
            Protocol = HttpProtocol.HTTP1_0;
            Method = HttpMethod.Get;
            body = "";
            rawBody = [];
            session = new Session(); //default, invalid session
            Terminal.INFO("Got an empty request, setting default values");
            return;
        }

        try
        {
            string[] firstLine = requestContent.First().Split(" ");
            Method = HttpUtils.GetMethod(firstLine[0]);
            Url = firstLine[1].Split("?")[0];
            if (!Url.StartsWith('/') && Url.EndsWith('/'))
            {
                //delete last "/" if url is like "example.com/"
                Url = Url[..^1];
            }
            Protocol = HttpUtils.GetProtocol(firstLine[2]);

            //collect parameters
            if (firstLine[1].Replace(Url, "") != "")
            {
                List<string> requestParameters = [.. firstLine[1].Split("?")[1].Split("&")];

                foreach (var p in requestParameters)
                {
                    if (p != "" && !parameters.ContainsKey(p)) //skip empty parameters and no duplicates
                        parameters.Add(p.Split("=")[0], p.Split("=")[1]);
                }
            }


            //collect headers
            foreach (string r in requestContent)
            {
                //skip if first element
                if (r == requestContent.First())
                    continue;
                if (r == "")
                {
                    break;
                }

                rawHeaders.Add(r);
                string[] header = r.Split(": ");
                if (header.Length == 2)
                    headers.Add(header[0], header[1]);
                else headers.Add(r, "");

            }

            //auth data collection
            //basic auth
            if (headers.TryGetValue("Authorization", out string? value))
            {
                var requestAuth = value;
                try
                {
                    requestAuth = Encoding.UTF8.GetString(Convert.FromBase64String(requestAuth));
                    var x = requestAuth.Split(":");
                    if (requestAuth.Length == 2)
                    {
                        basicAuth = new Tuple<string, string>(x[0], x[1]);
                    }

                }
                catch (Exception)
                {
                    if (requestAuth.Contains("Bearer"))
                    {
                        oAuth20Token = value;
                    }
                }
            }

            //oAuth1.0 information
            TryExtractAndSetOAuth1_0();

            //oAuth2.0 token 
            if (parameters.TryGetValue("access_token", out string? tkn))
            {
                oAuth20Token = tkn;
            }


            //parse cookies
            if (headers.TryGetValue("Cookie", out string? val))
            {

                var strings = val.Split("; ");
                foreach (var s in strings)
                {

                    cookies.Add(s.Split("=")[0], new Cookie(s));
                }
            }

            //search for a session token
            if (cookies.TryGetValue("hsbst", out Cookie? cookie) && SessionManager.GetInstance().IsValidSession(cookie.value))
            {
                session = SessionManager.GetInstance().GetSession(cookie.value);
            }
            else
            {

                session = new()
                {
                    ExpirationTime = DateTime.Now.AddTicks((long)config.DefaultSessionExpirationTime).Ticks
                };
                string sessionToken = SessionManager.GetInstance().CreateSession(session);

                Cookie c = new()
                {
                    name = "hsbst",
                    value = sessionToken,
                    expiration = DateTime.Now.AddTicks((long)config.DefaultSessionExpirationTime),
                    path = "/",
                    priority = Cookie.CookiePriority.HIGH
                };

                config.AddCustomGlobalCookie(c);
            }

            //extract body, which is the remaining part of the request text

            var offset = rawData.IndexOf("\r\n\r\n"u8.ToArray()) + 4;
            rawBody = rawData[offset..];
            body = Encoding.UTF8.GetString(rawBody);

            if (IsFileUpload())
            {
                multiPartFormData = new MultiPartFormData(rawBody, headers["Content-Type"].Split("boundary=")[1]);
            }
            if (IsFormUpload())
            {
                form = new Form(body);
            }

            ValidRequest = true;

        }
        catch (Exception e)
        {
            Terminal.WriteLine("Invalid request, reason : " + e.Message, BgColor.BLACK, FgColor.RED);
            ValidRequest = false;
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
    public Tuple<string, string>? GetBasicAuthInformation() => basicAuth;
    public OAuth10Information? GetOAuth1_0Information() => oAuth10Information;


    /// <summary>
    /// Test if a request contains a JSON document in the body
    /// </summary>
    /// <returns></returns>
    public bool IsJson() => headers["Content-Type"].StartsWith("application/json");
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
        headers.ContainsKey("Connection") && headers["Connection"].Equals("upgrade", StringComparison.CurrentCultureIgnoreCase) &&
        headers.ContainsKey("Upgrade") && headers["Upgrade"].Equals("websocket", StringComparison.CurrentCultureIgnoreCase);
    }
    /// <summary>
    /// Returns true if the request is a file upload
    /// </summary>
    /// <returns></returns>
    public bool IsFileUpload() => headers.ContainsKey("Content-Type") && headers["Content-Type"].StartsWith("multipart/form-data");
    /// <summary>
    /// Returns true if the request is a form upload
    /// </summary>
    /// <returns></returns>
    public bool IsFormUpload() => headers.ContainsKey("Content-Type") && headers["Content-Type"].StartsWith("application/x-www-form-urlencoded");
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
    internal string RawMethod => requestContent.First().Split(" ")[0];

    //utilities functions
    public void FullPrint()
    {
        Terminal.DEBUG("PRINTING RAW REQUEST\n====================");
        Terminal.INFO(reqText);
        Terminal.DEBUG("\n====================");
        Terminal.INFO($"Has basic auth? {basicAuth != null}");
        if (basicAuth != null)
            Terminal.INFO(basicAuth);
        Terminal.INFO($"Has oauth1.0? {oAuth10Information != null}");
        if (oAuth10Information != null)
            Terminal.INFO(oAuth10Information);

        Terminal.INFO($"Has oAuth2.0? {oAuth20Token != ""} {oAuth20Token}");

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
        _ => throw new Exception("Unsupported HTTP Protocol")
    };

}
