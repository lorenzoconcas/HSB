using System.Text;
using System.Text.Json;
using HSB.Constants.WebSocket;

namespace HSB.Components.WebSockets;

[Obsolete("Use WebSocketMessage with WebSocketConnection endpoint handlers instead.", false)]
public class Message
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        IncludeFields = true,
    };

    public byte[] data;
    public string _text;

    public Message(byte[]? data, string str)
    {
        this.data = data ?? Encoding.UTF8.GetBytes(str);
        _text = str;
    }

    public Message(Frame f)
    {
        data = f.GetPayload();
        _text = f.GetOpcode() == Opcode.TEXT ? Encoding.UTF8.GetString(f.GetPayload()) : "";
    }

    public string GetMessage()
    {
        return _text;
    }

    public string GetMessageBytes()
    {
        return Encoding.UTF8.GetString(data);
    }

    public object GetJSON()
    {
        return JsonSerializer.Deserialize<object>(_text, JsonSerializerOptions) ?? new object();
    }

    public void Dispose()
    {
        data = [];
        _text = "";
    }
}
