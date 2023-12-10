using System.Text;
using System.Text.Json;
using HSB.Constants.WebSocket;

namespace HSB.Components.WebSockets;

public class Message
{

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        IncludeFields = true,
    };


    public byte[] data;

    public string _text;

    public Message(byte[]? data, string str)
    {
        if (data == null)
        {
            this.data = Encoding.UTF8.GetBytes(str);
        }
        else
        {
            this.data = data;
        }
        _text = str;
    }

    public Message(Frame f)
    {
        data = f.GetPayload();
        if (f.GetOpcode() == Opcode.TEXT)
            _text = Encoding.UTF8.GetString(f.GetPayload());
        else _text = "";
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
        return JsonSerializer.Deserialize<object>(_text, jsonSerializerOptions) ?? new object();
    }


    public void Dispose()
    {
        data = [];
        _text = "";
    }

}