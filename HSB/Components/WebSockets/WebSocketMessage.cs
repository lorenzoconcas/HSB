using System.Text;

namespace HSB.Components.WebSockets;

public class WebSocketMessage
{
    public WebSocketMessage(byte[] raw, bool isText)
    {
        Raw = raw;
        IsText = isText;
        IsBinary = !isText;
        Text = isText ? Encoding.UTF8.GetString(raw) : "";
    }

    internal WebSocketMessage(Frame frame)
        : this(frame.GetPayload(), frame.GetOpcode() == Constants.WebSocket.Opcode.TEXT)
    {
    }

    public string Text { get; }
    public byte[] Raw { get; }
    public bool IsText { get; }
    public bool IsBinary { get; }
}
