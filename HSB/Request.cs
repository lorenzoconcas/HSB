using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace HSB
{

    public class Request
    {
        //support-variables
        readonly string reqText = "";
        readonly List<string> requestContent;
        internal Socket connectionSocket;
        private Configuration config;


        //Request variables
        public bool validRequest = false;
        HTTP_METHOD _method = HTTP_METHOD.UNKNOWN;
        HTTP_PROTOCOL _protocol = HTTP_PROTOCOL.UNKNOWN; //HTTP1.0 ecc
        string _url = "";
        string body = "";
        readonly Dictionary<string, string> headers = new();
        readonly Dictionary<string, string> parameters = new();
        readonly List<string> rawHeaders = new();
        readonly Dictionary<string, Cookie> cookies = new();

        private Session session = new();



        public Request(byte[] data, Socket socket, Configuration config)
        {
            connectionSocket = socket;
            this.config = config;
            requestContent = new();
            if (data == null)
            {

                return;
            }

            switch (Utils.GetEncoding(data))
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
                //i don't know why but the second request is invalid
                validRequest = false;
                config.debug.INFO("Got an invalid request, ignoring...");
                requestContent.Add(" ");
                return;
            }

            reqText = Encoding.UTF8.GetString(data);
            requestContent = reqText.Split("\r\n").ToList();
            ParseRequest();

        }

        private void ParseRequest()
        {
            if (reqText == "")
            {
                //empty request
                _url = "/";
                _protocol = HTTP_PROTOCOL.HTTP1_0;
                _method = HTTP_METHOD.GET;
                body = "";
                session = new Session(); //default, invalid session
                Terminal.INFO("Got an empty request, setting default values");
                return;
            }

            try
            {
                string[] firstLine = requestContent.First().Split(" ");
                _method = HttpUtils.GetMethod(firstLine[0]);
                _url = firstLine[1].Split("?")[0];
                _protocol = HttpUtils.GetProtocol(firstLine[2]);

                //collect parameters
                if (firstLine[1].Replace(_url, "") != "")
                {
                    List<string> prms = firstLine[1].Split("?")[1].Split("&").ToList();

                    foreach (string p in prms)
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
                    headers.Add(header[0], header[1]);

                }

                //parse cookies
                if (headers.ContainsKey("Cookie"))
                {
                    var cookieString = headers["Cookie"];
                    var strings = cookieString.Split("; ");
                    foreach (var s in strings)
                    {

                        cookies.Add(s.Split("=")[0], new Cookie(s));
                    }
                }

                //search for a session token
                if (cookies.ContainsKey("hsbst") && SessionManager.GetInstance().IsValidSession(cookies["hsbst"].value))
                {
                    session = SessionManager.GetInstance().GetSession(cookies["hsbst"].value);
                }
                else
                {

                    session = new()
                    {
                        ExpirationTime = DateTime.Now.AddTicks(config.defaultSessionExpirationTime).Ticks
                    };
                    string sessionToken = SessionManager.GetInstance().CreateSession(session);

                    Cookie c = new()
                    {
                        name = "hsbst",
                        value = sessionToken,
                        expiration = DateTime.Now.AddTicks(config.defaultSessionExpirationTime),
                        path = "/",
                        priority = Cookie.CookiePriority.HIGH
                    };

                    config.AddCustomGlobalCookie(c);
                }

                //extract body
                body = requestContent.Last();

            }
            catch (Exception e)
            {
                Terminal.WriteLine("Invalid request, reason : " + e.Message, BG_COLOR.BLACK, FG_COLOR.RED);
                validRequest = false;
                return;
            }
            validRequest = true;

        }

        public override string ToString()
        {
            string str = _method.ToString() + " - " + _url + " - " + _protocol.ToString();
            return str;
        }
        public HTTP_METHOD METHOD => _method;
        public HTTP_PROTOCOL PROTOCOL => _protocol;
        public string URL => _url;
        public string RawBody => body;
        public Dictionary<string, string> GetHeaders => headers;
        public List<string> GetRawHeaders => rawHeaders;
        public Dictionary<string, string> GetParameters => parameters;
        public Session GetSession() => session;
        internal string GetRawRequest => reqText;
        internal string RawMethod => requestContent.First().Split(" ")[0];

        //utilities functions

        /// <summary>
        /// Test if a request contains a JSON document in the body
        /// </summary>
        /// <returns></returns>
        public bool IsJSON() => headers["Content-Type"].StartsWith("application/json");


    }


    public static class HttpUtils
    {
        public static string MethodAsString(HTTP_METHOD method) => method switch
        {
            HTTP_METHOD.GET => "GET",
            HTTP_METHOD.POST => "POST",
            HTTP_METHOD.PUT => "PUT",
            HTTP_METHOD.DELETE => "DELETE",
            HTTP_METHOD.HEAD => "HEAD",
            HTTP_METHOD.PATCH => "PATCH",
            HTTP_METHOD.OPTIONS => "OPTIONS",
            HTTP_METHOD.TRACE => "TRACE",
            HTTP_METHOD.CONNECT => "CONNECT",
            _ => "GET", //failsafe?
        };

        public static string ProtocolAsString(HTTP_PROTOCOL protocol) => protocol switch
        {
            HTTP_PROTOCOL.HTTP1_0 => "HTTP/1.0",
            HTTP_PROTOCOL.HTTP1_1 => "HTTP/1.1",
            HTTP_PROTOCOL.HTTP2_0 => "HTTP/2.0",
            HTTP_PROTOCOL.HTTP3_0 => "HTTP/3.0",
            _ => "HTTP/1.0",
        };
        public static HTTP_METHOD GetMethod(string data) => data switch
        {
            "GET" => HTTP_METHOD.GET,
            "POST" => HTTP_METHOD.POST,
            "PUT" => HTTP_METHOD.PUT,
            "DELETE" => HTTP_METHOD.DELETE,
            "HEAD" => HTTP_METHOD.HEAD,
            "PATCH" => HTTP_METHOD.PATCH,
            "OPTIONS" => HTTP_METHOD.OPTIONS,
            "TRACE" => HTTP_METHOD.TRACE,
            "CONNECT" => HTTP_METHOD.CONNECT,
            _ => HTTP_METHOD.UNKNOWN
        };

        public static HTTP_PROTOCOL GetProtocol(string data) => data switch
        {
            "HTTP/1.0" => HTTP_PROTOCOL.HTTP1_0,
            "HTTP/1.1" => HTTP_PROTOCOL.HTTP1_1,
            "HTTP/2.0" => HTTP_PROTOCOL.HTTP2_0,
            "HTTP/3.0" => HTTP_PROTOCOL.HTTP3_0,
            _ => throw new Exception("Unsupported HTTP Protocol")
        };

    }

}
