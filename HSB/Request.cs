using System.Net.Sockets;
using System.Text;


namespace HSB
{
    public struct OAuth1_0Information
    {
        public readonly string access_token;
        public readonly string nonce;
        public readonly string token;
        public readonly string version;
        public readonly string signature_method;
        public readonly string timestamp;
        public readonly string consumer_key;
        public readonly string signature;

        public OAuth1_0Information(Dictionary<string, string> parameters)
        {
            access_token = parameters.TryGetValueFromDict("access_token", "");
            nonce = parameters.TryGetValueFromDict("oauth_nonce", "");
            token = parameters.TryGetValueFromDict("oauth_token", "");
            version = parameters.TryGetValueFromDict("oauth_version", "");
            signature_method = parameters.TryGetValueFromDict("oauth_signature_method", "");
            timestamp = parameters.TryGetValueFromDict("oauth_timestamp", "");
            consumer_key = parameters.TryGetValueFromDict("oauth_consumer_key", "");
            signature = parameters.TryGetValueFromDict("oauth_signature", "");
        }

        public readonly bool IsValid()
        {
            return access_token != "" && nonce != "" && token != "" && version != "" && signature_method != "" && timestamp != "" && consumer_key != "" && signature != "";
        }

        public override readonly string ToString()
        {
            return IsValid() ? $"Valid oAuth1.0 with timestamp {timestamp}, version {version}" : "No valid oAuth1.0 found";
        }

    }

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
        //Auth structs
        private Tuple<string, string>? basicAuth;
        private OAuth1_0Information? oAuth1_0Information;
        private string oAuth2_0Token = "";
        private Session session = new();

        //WIP for TLS support
        private bool IsTLS;
        //private TLS.ProtocolVersion ProtocolVersion;


        public bool proceedWithElaboration = true;
        public Request(byte[] data, Socket socket, Configuration config)
        {
            connectionSocket = socket;
            this.config = config;
            requestContent = new();
            if (data == null)
            {
                return;
            }

            IsTLS = data[0] == 22;//(int)TLS.ContentType.Handshake;

            if (IsTLS)
            {
                proceedWithElaboration = false;
                return;
                // ParseTLSRequest(data);
            }
            else
            {
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

        }

        private void ParseTLSRequest(byte[] data)
        {
            throw new NotImplementedException();
        }

        private void SendKey()
        {
            throw new NotImplementedException();
        }

        private void SendCertificate()
        {
            throw new NotImplementedException();
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
                    if (header.Length == 2)
                        headers.Add(header[0], header[1]);
                    else headers.Add(r, "");

                }

                //auth data collection
                //basic auth
                if (headers.ContainsKey("Authorization"))
                {
                    var _auth = headers["Authorization"];
                    try
                    {
                        _auth = Encoding.UTF8.GetString(Convert.FromBase64String(_auth));
                        var x = _auth.Split(":");
                        if (_auth.Length == 2)
                        {
                            basicAuth = new(x[0], x[1]);
                        }

                    }
                    catch (Exception)
                    {
                        if (_auth.Contains("Bearer"))
                        {
                            oAuth2_0Token = headers["Authorization"];
                        }
                    }
                }

                //oAuth1.0 information
                TryExtractAndSetOAuth1_0();

                //oAuth2.0 token 
                if (parameters.ContainsKey("access_token"))
                {
                    oAuth2_0Token = parameters["access_token"];
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
                        ExpirationTime = DateTime.Now.AddTicks((long)config.defaultSessionExpirationTime).Ticks
                    };
                    string sessionToken = SessionManager.GetInstance().CreateSession(session);

                    Cookie c = new()
                    {
                        name = "hsbst",
                        value = sessionToken,
                        expiration = DateTime.Now.AddTicks((long)config.defaultSessionExpirationTime),
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

        private void TryExtractAndSetOAuth1_0()
        {
            OAuth1_0Information data = new(parameters);
            if (data.IsValid())
                oAuth1_0Information = data;

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
        public Tuple<string, string>? GetBasicAuthInformation() => basicAuth;
        internal string GetRawRequest => reqText;
        internal string RawMethod => requestContent.First().Split(" ")[0];

        public void FullPrint()
        {
            Terminal.DEBUG("PRINTING RAW REQUEST\n====================");
            Terminal.INFO(reqText);
            Terminal.DEBUG("\n====================");
            Terminal.INFO($"Has basic auth? {basicAuth != null}");
            if (basicAuth != null)
                Terminal.INFO(basicAuth);
            Terminal.INFO($"Has oauth1.0? {oAuth1_0Information != null}");
            if (oAuth1_0Information != null)
                Terminal.INFO(oAuth1_0Information);

            Terminal.INFO($"Has oAuth2.0? {oAuth2_0Token != ""} {oAuth2_0Token}");

        }









































































































































































































































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
