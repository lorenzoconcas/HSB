using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;

namespace HSB
{

    public class Request
    {
        //support-variables
        readonly string reqText = "";
        readonly List<string> requestContent;
        internal Socket connectionSocket;

        //Request variables
        public bool validRequest = false;
        HTTP_METHOD _method = HTTP_METHOD.UNKNOWN;
        HTTP_PROTOCOL _protocol = HTTP_PROTOCOL.UNKNOWN; //HTTP1.0 ecc
        string _url = "";
        string body = "";
        readonly Dictionary<string, string> headers = new();
        readonly Dictionary<string, string> parameters = new();
        readonly List<string> rawHeaders = new();


        public Request(byte[] data, Socket socket)
        {
            connectionSocket = socket;
            if (data == null)
            {
                requestContent = new();
                return;
            }
            reqText = Encoding.UTF8.GetString(data);
            requestContent = reqText.Split("\r\n").ToList();

            ParseRequest();

        }
        private void ParseRequest()
        {
            try
            {
                string[] firstLine = requestContent.First().Split(" ");
                _method = HttpUtils.GetMethod(firstLine[0]);
                _url = firstLine[1].Split("?")[0];
                _protocol = HttpUtils.GetProtocol(firstLine[2]);

                if (firstLine[1].Replace(_url, "") != "")
                {
                    List<string> prms = firstLine[1].Split("?")[1].Split("&").ToList();

                    foreach (string p in prms)
                    {
                        if (p != "")
                            parameters.Add(p.Split("=")[0], p.Split("=")[1]);
                    }
                }


                //get headers
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
                body = requestContent.Last();

            }
            catch (Exception e)
            {
                Terminal.WriteLine("Invalid request, reason : " + e.Message, BG_COLOR.NERO, FG_COLOR.ROSSO);
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
        internal string GetRawRequest => reqText;


        //utilities functions
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
            _ => throw new Exception("Unsupported Http Method"),
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
