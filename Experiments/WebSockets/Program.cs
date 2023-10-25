/*
* This code is used to implement the WebSocket functionality.
* This project will be deleted once is ready and moved to the main library project.
* Requires HSB commit 93aca37 or later.
*/

using System.Text;
using HSB;
using System.Security.Cryptography;
using HSB.Constants;
using System.Net.Sockets;
using System.Collections;
using HSB.Components.WebSockets;

//some tests
/*
var val = new bool[]{
    false, //0 
    false, //1
    false, //2
    false, //3
    false, //4
    true, //5
    FtpStyleUriParser, //6
};


Terminal.INFO(val.ToInt());
Terminal.INFO(2 == val.ToInt());
return;*/

Configuration c = new()
{
    port = 8080,
    address = "127.0.0.1",
    requestMaxSize = Configuration.KILOBYTE * 1,
};

c.GET("/", (Request req, Response res) =>
{
    if (req.IsWebSocket())
    {
        Console.WriteLine("Websocket request");

        var headers = req.Headers;

        //those two values are required, if they are missing -> 400
        if (!headers.ContainsKey("Sec-WebSocket-Key") && !headers.ContainsKey("Sec-WebSocket-Extensions"))
        {
            res.SendCode(HTTP_CODES.BAD_REQUEST);
            return;
        }

        string SecWebSocketKey = headers["Sec-WebSocket-Key"];
        string[] SecWebSocketExtensions = headers["Sec-WebSocket-Extensions"].Split("; ") ?? Array.Empty<string>();

        string SecWebSocketProtocol = headers.GetValueOrDefault("Sec-WebSocket-Protocol", "");
        string SecWebSocketVersion = headers.GetValueOrDefault("Sec-WebSocket-Version", "");

        string acceptSHA1Value = Convert.ToBase64String(
            SHA1.HashData(
                Encoding.UTF8.GetBytes(SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
            ));
        var socket = req.GetSocket();
        var buffer = new byte[Configuration.KILOBYTE * 1];

        List<string> response = new(){
            "HTTP/1.1 101 Switching Protocols\r\n",
            "Upgrade: websocket\r\n",
            "Connection: Upgrade\r\n",
            "Sec-WebSocket-Accept: " + acceptSHA1Value + "\r\n",
            //  "Sec-WebSocket-Extensions: " + SecWebSocketExtensions[0] + "\r\n" +
            //   "Sec-WebSocket-Protocol: " + SecWebSocketProtocol + "\r\n" +
            "Sec-WebSocket-Version: " + SecWebSocketVersion + "\r\n",
        };
        // "\r\n";


        if (SecWebSocketProtocol != "") //if the protocol is not empty, add it to the response
            response.Add("Sec-WebSocket-Protocol: " + SecWebSocketProtocol + "\r\n");

        response.Add("\r\n");

        var responseString = string.Join("", response);

        socket.Send(Encoding.UTF8.GetBytes(responseString));

        var msg = Encoding.UTF8.GetBytes("Hello from the HSB-#!");
        var size = BitConverter.GetBytes(msg.Length);


        /*   Frame firstFrame = new Frame();
           firstFrame.SetOpcode(Opcode.TEXT);
           firstFrame.SetPayload(msg);*/


        //  var bits = new BitArray(
        var bits = new List<bool>(){
            true, //FIN, true if is the last frame, false if there are more frames to come
            false, //RSV1, false
            false, //RSV2, false
            false,  //RSV2, false
            false, false, false, true, //Opcode, 1 = text
            false, //Mask, false if the payload is not masked
        };
        bits.AddRange(Utils.IntTo7Bits(msg.Length));

        var _data = new BitArray(bits.ToArray());

        var data = new List<byte>();
        for (int i = 0; i < _data.Length; i += 8)
        {
            bool[] _byte = new bool[8];
            for (int j = 0; j < 8; j++)
            {
                _byte[j] = _data[i + j];
            }
            data.Add((byte)Convert.ToInt32(string.Join("", _byte.Select(x => x ? 1 : 0)), 2));
        }
        data.AddRange(msg);




        socket.Send(data.ToArray());

        while (true)
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback((IAsyncResult ar) =>
            {
                var socket = (Socket)ar.AsyncState;
                int received = socket.EndReceive(ar);

                Frame f = new(buffer[..received]);
                Terminal.INFO(f);
                var b = f.GetPayload();
                Terminal.INFO(Encoding.UTF8.GetString(b));
              
            }), socket);
        }


    }
    else
    {
        var html = $"<html></head></head><body>\n";
        html += "<label id=\"status\">Status: Disconnected</label>\n";
        html += "<button onclick=\"ws = new WebSocket('ws://localhost:8080'); ws.onopen = function(){document.getElementById('status').innerHTML = 'Status: Connected'}; ws.onmessage = function(e){document.getElementById('messages').value += 'server: '+ e.data + '\\n'}; ws.onclose = function(){document.getElementById('status').innerHTML = 'Status: Disconnected'};\">Connect</button>\n";
        html += "<textarea id=\"messages\" style=\"width: 100%; height: 200px;\"></textarea>\n";
        html += "<input type=\"text\" id=\"message\" style=\"width: 100%;\" />\n";
        html += "<button onclick=\"ws.send(document.getElementById('message').value); document.getElementById('messages').value += 'client: '+ document.getElementById('message').value + '\\n';\">Send</button>\n";
        html += "</body>";

        res.SendHTMLContent(html);

    }


});

new Server(c).Start();

