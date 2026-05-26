using HSB;
using HSB.Components.Controller;
using HSB.Components.WebSockets;

namespace Runner;

[Controller("")]
public class WebSocketController
{
    [Ws("/websocket")]
    private void Echo(WebSocketConnection socket)
    {
        socket.OnOpen(() => Terminal.Info("New websocket connection opened"));

        socket.OnMessage(msg =>
        {
            Terminal.Debug($"Got message : {msg.Text}");
            socket.Send(msg.Text);
        });

        socket.OnClose(() => Terminal.Info("Websocket disconnected"));
    }
}
