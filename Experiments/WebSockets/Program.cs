/*
* This code is used to implement the WebSocket functionality.
* This project will be deleted once is ready and moved to the main library project.
* Requires HSB commit 93aca37 or later.
*/

using System.Text;
using HSB;
using System.Security.Cryptography;


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
        string SecWebSocketKey = req.GetHeaders["Sec-WebSocket-Key"] ?? "";
        string[] SecWebSocketExtensions = req.GetHeaders["Sec-WebSocket-Extensions"].Split("; ") ?? Array.Empty<string>();
        string SecWebSocketProtocol = req.GetHeaders["Sec-WebSocket-Protocol"] ?? "";
        string SecWebSocketVersion = req.GetHeaders["Sec-WebSocket-Version"] ?? "";
        Console.WriteLine("{\n\t" + req.GetRawRequestText.Replace("\r\n", "\r\n\t") + "\n}");

        string acceptSHA1Value = Convert.ToBase64String(
            SHA1.HashData(
                Encoding.UTF8.GetBytes(SecWebSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")
            ));

        string response = "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + acceptSHA1Value + "\r\n" +
            "Sec-WebSocket-Extensions: " + SecWebSocketExtensions[0] + "\r\n" +
            "Sec-WebSocket-Protocol: " + SecWebSocketProtocol + "\r\n" +
            "Sec-WebSocket-Version: " + SecWebSocketVersion + "\r\n" +
            "\r\n";

        Console.WriteLine("Response preview:\n{\n" + response + "\n}");
        res.Send(Encoding.UTF8.GetBytes(response), false);


    }
    else
    {
        Console.WriteLine("Normal request");
        res.SendHTMLContent("<h1>Hello from HSB</h1>");
    }


});

new Server(c).Start();
