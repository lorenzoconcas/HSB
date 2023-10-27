/*
* This code is used to implement the WebSocket functionality.
* This project will be deleted once is ready and moved to the main library project.
* Requires HSB commit 93aca37 (v0.0.8 stable) or later
* note: This project will be deleted from repository after the next alpha is released (v0.0.11?)
*/

using System.Text;
using HSB;
using System.Security.Cryptography;
using HSB.Constants;
using System.Net.Sockets;
using HSB.Components.WebSockets;
using System.Reflection.Emit;

Configuration c = new()
{
    port = 8081,
    address = "127.0.0.1",
    requestMaxSize = Configuration.KILOBYTE * 1,
};

c.GET("/", (Request req, Response res) =>
{
    if (req.IsWebSocket())
    {
        Console.WriteLine("Received a websocket request");

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
            "Sec-WebSocket-Version: " + SecWebSocketVersion + "\r\n",
        };

        if (SecWebSocketProtocol != "") //if the protocol is not empty, add it to the response
            response.Add("Sec-WebSocket-Protocol: " + SecWebSocketProtocol + "\r\n");

      /*   if (SecWebSocketExtensions.Length > 0)
            response.Add("Sec-WebSocket-Extensions: " + "\r\n");*/

        response.Add("\r\n");

        var responseString = string.Join("", response);
        socket.Send(Encoding.UTF8.GetBytes(responseString));

        Frame helloMessage = new();
        helloMessage.SetPayload("server says: Hello from the HSB-# (sent using WebSockets)");
        //48 65 6C 6C 6F 20 66 72 6F 6D 20 74 68 65 20 48 53 42 2D 23
        //H  e  l  l  o  [] f  r  o  m  []  t  h  e [] H  S  B  -  # 
        //bytes count : 20
        Terminal.INFO("HelloMessage: " + helloMessage);
        socket.Send(helloMessage.Build());



        while (true)
        {
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback((IAsyncResult ar) =>
            {
                var socket = (Socket?)ar.AsyncState;
                if(socket == null) return;
                int received = socket.EndReceive(ar);
                if(received < 2) return; //not a valid frame
                Frame f = new(buffer[..received]);
                Opcode opcode = f.GetOpcode();

                switch(f.GetOpcode()){
                    case Opcode.CLOSE:{
                        Terminal.INFO("Closing connection websocket");
                        socket.Close();
                        return;
                    }
                    case Opcode.BINARY:{
                        Terminal.INFO("Received a binary frame: " + f);
                        var payload = f.GetPayload();
                        Terminal.INFO("Payload: 0x" + BitConverter.ToString(payload).Replace("-", " 0x"));
                        return;
                    }
                    case Opcode.TEXT:{
                        //if a frame is TEXT the encoding is UTF-8
                        
                        var payload = f.GetPayload();
                        var str = Encoding.UTF8.GetString(payload);
                        Terminal.INFO("Received a text frame: " + f);
                        Terminal.INFO("Content: " + payload);
                        //Send server echo!
                        Frame echo = new();
                        echo.SetPayload("server echo: " + str);
                        socket.Send(echo.Build());
                        return;
                    }
                }


            }), socket);
        }


    }
    else
    {
        var html = $"<html></head></head><body>\n";
        html += "<label id=\"status\">Status: Disconnected</label>\n";
        html += "<button onclick=\"ws = new WebSocket('ws://localhost:8081'); ws.onopen = function(){document.getElementById('status').innerHTML = 'Status: Connected'}; ws.onmessage = function(e){document.getElementById('messages').value += e.data + '\\n'}; ws.onclose = function(){document.getElementById('status').innerHTML = 'Status: Disconnected'};\">Connect</button>\n";
        html += "<textarea id=\"messages\" style=\"width: 100%; height: 200px;\"></textarea>\n";
        html += "<input type=\"text\" id=\"message\" style=\"width: 100%;\" />\n";
        html += "<button onclick=\"ws.send(document.getElementById('message').value); document.getElementById('messages').value += 'client: '+ document.getElementById('message').value + '\\n';\">Send</button>\n";
        html += "</body>";

        res.SendHTMLContent(html);

    }


});

new Server(c).Start();

