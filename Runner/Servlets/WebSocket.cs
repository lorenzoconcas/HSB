using HSB;
using HSB.Components.WebSockets;

namespace Runner;

[Binding("/websocket")]
public class WebSocketHandler : WebSocket
{
    public WebSocketHandler(Request req, Response res, Configuration c) : base(req, res, c)
    {

    }

    public override void OnOpen()
    {
        Terminal.INFO("New websocket connection opened");
    }

    public override void OnMessage(Message msg)
    {
        //echo the message
        Terminal.DEBUG($"Got message : {msg.GetMessage()}");
        Send(msg);
        Send("ciao");
    }

    public override void OnClose()
    {
        Terminal.INFO("Websocket disconnected");
    }

}

