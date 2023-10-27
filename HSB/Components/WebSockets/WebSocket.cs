using System.Collections.Immutable;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HSB.Constants;
using HSB.Constants.WebSocket;

namespace HSB.Components.WebSockets;

public class WebSocket
{

    protected Response res;
    protected Request req;
    private Socket socket;
    protected Configuration? c;

    private WebSocketState state = WebSocketState.CLOSED;

    //acceptance requirements
    Dictionary<string, string> requiredHeaders;
    Dictionary<string, string> requiredParams;
    string bearerToken = "";
    string oAuth2Token = "";
    Tuple<string, string>? basicAuth = null;
    OAuth1_0Information? oAuth1_0Information = null;

    private byte[] messageSentOnOpen = Array.Empty<byte>();

    public WebSocket(Request req, Response res, Configuration? c = null)
    {
        this.req = req;
        this.res = res;
        this.c = c;
        socket = req.GetSocket();
        requiredHeaders = new();
        requiredParams = new();

    }
    /// <summary>
    /// Returns the Base64 string of the SecWebSocketKey+WS_GUID
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private static string DigestKey(string key)
    {
        return Convert.ToBase64String(
            SHA1.HashData(
                Encoding.UTF8.GetBytes(key + WebSocketsContants.WS_GUID)
            ));
    }
    /// <summary>
    /// Accept the ws connection request
    /// </summary>
    /// <returns></returns>
    private bool Accept()
    {

        if (!req.IsWebSocket()) //not a websocket request, we cannot do anything here
        {
            c?.debug.WARNING("Not a websocket request, this code should never be reached");
            res.SendCode(HTTP_CODES.BAD_REQUEST);
            return false;
        }


        var headers = req.Headers;
        //those two values are required, if they are missing -> 400
        if (!headers.ContainsKey("Sec-WebSocket-Key") && !headers.ContainsKey("Sec-WebSocket-Version"))
        {
            c?.debug.WARNING("Missing Sec-WebSocket-Key or Sec-WebSocket-Version, malformed request");
            res.SendCode(HTTP_CODES.BAD_REQUEST);
            return false;
        }

        //check if request contains all the required headers
        if (requiredHeaders.Any())
        {
            foreach (var header in requiredHeaders)
            {
                if (!headers.ContainsKey(header.Key) || headers[header.Key] != header.Value)
                {
                    c?.debug.WARNING($"Missing required header {header.Key} or value is not correct");
                    res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                    return false;
                }
            }
        }
        //same for parameters
        if (requiredParams.Any())
        {
            foreach (var param in requiredParams)
            {
                if (!req.Parameters.ContainsKey(param.Key) || req.Parameters[param.Key] != param.Value)
                {
                    c?.debug.WARNING($"Missing required parameter {param.Key} or value is not correct");
                    res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                    return false;
                }
            }
        }
        //if an authentication method is set, check if it is correct
        if (bearerToken != "")
        {
            if (!headers.ContainsKey("Authorization") || headers["Authorization"] != $"Bearer {bearerToken}")
            {
                c?.debug.WARNING($"Missing or incorrect Authorization header");
                res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                return false;
            }
        }
        if (oAuth2Token != "")
        {
            if (!headers.ContainsKey("Authorization") || headers["Authorization"] != $"OAuth {oAuth2Token}")
            {
                c?.debug.WARNING($"Missing or incorrect Authorization header");
                res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                return false;
            }
        }
        if (basicAuth != null)
        {
            var bAuth = req.GetBasicAuthInformation();
            if (bAuth == null || bAuth.Item1 != basicAuth.Item1 || bAuth.Item2 != basicAuth.Item2)
            {
                c?.debug.WARNING($"Missing or incorrect Authorization header");
                res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                return false;
            }
        }
        if (oAuth1_0Information != null)
        {
            var oAuth1 = req.GetOAuth1_0Information();
            if (oAuth1 == null || oAuth1.Equals(oAuth1))
            {
                c?.debug.WARNING($"Missing or incorrect Authorization header");
                res.SendCode(HTTP_CODES.BAD_REQUEST); //is this correct?
                return false;
            }
        }


        state = WebSocketState.CONNECTING;

        var clientKey = headers["Sec-WebSocket-Key"];
        var clientVersion = headers["Sec-WebSocket-Version"];

        var key = DigestKey(clientKey);
        List<string> response = new() {
            "HTTP/1.1 101 Switching Protocols\r\n",
            "Upgrade: websocket\r\n",
            "Connection: Upgrade\r\n",
            "Sec-WebSocket-Accept: " + key + "\r\n",
            "Sec-WebSocket-Version: " + clientVersion + "\r\n",
        };

        //to-do add protocol support via decorator

        var protocol = headers.GetValueOrDefault("Sec-WebSocket-Protocol", "");

        if (protocol != "") //if the protocol is not empty, add it to the response
            response.Add("Sec-WebSocket-Protocol: " + protocol + "\r\n");

        response.Add("\r\n");

        socket?.Send(Encoding.UTF8.GetBytes(string.Join("", response)));

        state = WebSocketState.OPEN;


        if (messageSentOnOpen.Length > 0)
        {
            Send(messageSentOnOpen);
        }


        new Task(() =>
        {
            OnOpen();
        }).Start();

        return true;
    }
    private void MessageLoop()
    {
        var buffer = new byte[Configuration.KILOBYTE * 32];

        while (state == WebSocketState.OPEN)
        {
            try
            {


                socket?.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback((IAsyncResult ar) =>
                {
                    var socket = (Socket?)ar.AsyncState;
                    if (socket == null)
                    {
                        Terminal.DEBUG("socket is null??");
                        return;
                    }
                    int received = 0;
                    try
                    {
                        received = socket.EndReceive(ar);
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    if (received < 2)
                    {
                        Terminal.DEBUG("wrong data length?? -> " + received);
                        return;
                    }
                    Frame f = new(buffer[..received]);
                    Opcode opcode = f.GetOpcode();

                    switch (f.GetOpcode())
                    {
                        case Opcode.CLOSE:
                            this.Close();
                            state = WebSocketState.CLOSED;
                            return;
                        //todo -> check Ping e Pong correctness
                        case Opcode.PING:
                            Frame pong = new();
                            pong.SetOpcode(Opcode.PONG);
                            socket.Send(pong.Build());
                            break;
                        case Opcode.PONG:
                            Frame ping = new();
                            ping.SetOpcode(Opcode.PING);
                            socket.Send(ping.Build());
                            break;
                        case Opcode.TEXT:
                        case Opcode.BINARY:
                            {
                                OnMessage(new(f));
                                break;
                            }
                    }

                }), socket);
            }
            catch (Exception)
            {
                state = WebSocketState.CLOSED;
            }
        }
    }

    //public methods

    //this is should be use only by HSB/Server.cs or HSB/Configuration.cs
    public void Process()
    {
        var frame = new System.Diagnostics.StackTrace().GetFrame(1);
        var method = frame?.GetMethod();
        var rfn = method?.ReflectedType?.Name ?? "";
        var callerName = method?.Name ?? "";
        if(rfn != "Server" && callerName != "ProcessRequest")
        {
            throw new Exception("This function must be called ONLY by the server process");
        }            

        if (Accept())
        {
            new Thread(MessageLoop).Start();
        }
    }
    /// <summary>
    /// Sets a message to be sent to the client when the connection is open
    /// </summary>
    /// <param name="data"></param>
    public void SetMessageSentOnOpen(byte[] data)
    {
        messageSentOnOpen = data;
    }
    /// <summary>
    /// Sends a message to the client when the connection is open
    /// </summary>
    /// <param name="data"></param>
    /// <exception cref="Exception">If connection is not opened</exception>
    public void Send(byte[] data)
    {
        if (state == WebSocketState.OPEN)
        {
            Frame f = new();
            f.SetPayload(data);
            socket?.Send(f.Build());
        }
        else
        {
            throw new Exception("WebSocket is not connected");
        }
    }
    /// <summary>
    /// Sends a message to the client when the connection is open
    /// </summary>
    /// <param name="message"></param>
    /// <exception cref="Exception"></exception>
    public void Send(string message)
    {
        if (state == WebSocketState.OPEN)
        {
            Frame f = new();
            f.SetPayload(message);
            socket?.Send(f.Build());
        }
        else
        {
            throw new Exception("WebSocket is not connected");
        }
    }
    public void Send(Message msg)
    {
        if (state == WebSocketState.OPEN)
        {
            Frame f = new();
            if (msg.GetMessage() != "")
                f.SetPayload(msg.GetMessage());
            else if (msg.GetMessageBytes() != "")
                f.SetPayload(msg.GetMessageBytes());
            socket?.Send(f.Build());
        }
        else
        {
            throw new Exception("WebSocket is not connected");
        }
    }
    /// <summary>
    /// Sends an object to the client as a json string
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="includeFields"></param>
    /// <exception cref="Exception"></exception>
    public void Send<T>(T obj, bool includeFields = true)
    {
        if (state == WebSocketState.OPEN)
        {
            Frame f = new();
            //serialize object to json, with fields
            JsonSerializerOptions jo = new()
            {
                IncludeFields = includeFields,
                MaxDepth = 0
            };

            f.SetPayload(JsonSerializer.SerializeToUtf8Bytes(obj, jo));
            socket?.Send(f.Build());
        }
        else
        {
            throw new Exception("WebSocket is not connected");
        }
    }
    public void Close()
    {
        if (state == WebSocketState.OPEN)
        {
            Frame f = new();
            f.SetOpcode(Opcode.CLOSE);
            socket?.Send(f.Build());
            socket?.Close();
            
            OnClose();
        }
        else
        {
            throw new Exception("WebSocket is not connected, cannot close");
        }
    }
    public void SetConnectionRequirements(
        Dictionary<string, string> requiredHeaders,
        Dictionary<string, string> requiredParams,
        string bearerToken = "",
        string oAuth2Token = "",
        Tuple<string, string>? basicAuth = null,
        OAuth1_0Information? oAuth1_0Information = null
    )
    {
        //todo -> accept only one auth type
        this.requiredHeaders = requiredHeaders;
        this.requiredParams = requiredParams;
        this.bearerToken = bearerToken;
        this.oAuth2Token = oAuth2Token;
        this.basicAuth = basicAuth;
        this.oAuth1_0Information = oAuth1_0Information;

    }
    //events
    public virtual void OnMessage(Message msg) { }

    public virtual void OnOpen() { }
    /// <summary>
    /// OnClose is called after a close request is received from the client, to directly close the connection use Close()
    /// </summary>
    public virtual void OnClose() { }
}

