using System.Collections;
using System.Text;
using HSB.Constants.WebSocket;
namespace HSB.Components.WebSockets;

public class Frame
{
    private bool FIN { get; set; }
    private bool RSV1 { get; set; }
    private bool RSV2 { get; set; }
    private bool RSV3 { get; set; }
    private bool[]? Opcode { get; set; }
    private bool Mask { get; set; }
    private bool[] PayloadLength;
    private byte[]? ExtendedPayloadLength { get; set; } //two bytes (16bit)
    private byte[]? ExtendedPayloadLengthContinued { get; set; } //8 bytes (64bit)
    private byte[]? MaskingKey { get; set; }
    private byte[]? MaskingKeyContinued { get; set; }
    private byte[]? PayloadData { get; set; }

    /// <summary>
    /// Use this constructor to create a frame
    /// </summary>
    /// <param name="fin"></param>
    /// <param name="rsv1"></param>
    /// <param name="rsv2"></param>
    /// <param name="rsv3"></param>
    /// <param name="opcode"></param>
    /// <param name="mask"></param>
    public Frame(bool fin = true, bool rsv1 = false, bool rsv2 = false, bool rsv3 = false, Opcode opcode = Constants.WebSocket.Opcode.TEXT, bool mask = false)
    {
        FIN = fin;
        RSV1 = rsv1;
        RSV2 = rsv2;
        RSV3 = rsv3;
        SetOpcode(opcode);
        Mask = mask;
        PayloadLength = [false, false, false, false, false, false, false];
    }

    /// <summary>
    /// Use this costructor to decode a frame
    /// </summary>
    /// <param name="data"></param>
    public Frame(byte[] data)
    {
        //minimum length of a frame is 2 bytes
        if (data.Length < 2)
            throw new Exception("Frame: data.Length < 2");

        var first8bits = data[0].ToBitArray();
        FIN = first8bits[0];
        RSV1 = first8bits[1];
        RSV2 = first8bits[2];
        RSV3 = first8bits[3];
        Opcode = first8bits[4..];
        var maskAndPayloadLength = data[1].ToBitArray();
        Mask = maskAndPayloadLength[0];
        PayloadLength = maskAndPayloadLength[1..];

        if (PayloadLength.ToInt() == 126)
        {
            //ExtendedPayloadLength is present
            if (data.Length < 4)
                throw new Exception("Frame: data.Length < 4 and Payload length is 126");
            ExtendedPayloadLength = [data[2], data[3]];
        }
        else if (PayloadLength.ToInt() == 127)
        {
            //ExtendedPayloadLengthContinued is present
            if (data.Length < 10)
                throw new Exception("Frame: data.Length < 10 and Payload length is 127");
            ExtendedPayloadLengthContinued = [data[2], data[3], data[4], data[5], data[6], data[7], data[8], data[9]];
        }

        //masking key
        int offset = 0; //from the start of the frame byte data
        if (Mask)
        {
            //Mask position dependd on the size of the payload

            if (PayloadLength.ToInt() < 126) //ExtendedPayloadLength and ExtendedPayloadLengthContinued are not present
                offset = 2;
            if (PayloadLength.ToInt() == 126)
                offset = 4;
            if (PayloadLength.ToInt() == 127)
                offset = 10; //{flags, opcode, mask, payload length} = 2, extended payload length = 8

            if (data.Length < offset + 4)
                throw new Exception($"Frame: data.Length < {offset} + 4 and a mask is present");

            MaskingKey = data[offset..(offset + 4)];
            offset += 4;            
        }

        PayloadData = data[offset..];      
    }
    /// <summary>
    /// Build the frame
    /// </summary>
    /// <returns></returns>
    public byte[] Build()
    {
        List<byte> bytes = [
            Utils.GetByte(
                FIN,
                RSV1,
                RSV2,
                RSV3,
                Opcode?[0] ?? false,
                Opcode?[1] ?? false,
                Opcode?[2] ?? false,
                Opcode?[3] ?? false
            ),
            Utils.GetByte(
                //mask and payload length
                Mask,
                PayloadLength[0],
                PayloadLength[1],
                PayloadLength[2],
                PayloadLength[3],
                PayloadLength[4],
                PayloadLength[5],
                PayloadLength[6]
            )
        ];
        if (ExtendedPayloadLength != null) //two bytes
        {
            Terminal.INFO("This message has an extended payload length of 2 bytes");
            bytes.AddRange(ExtendedPayloadLength);
        }
        if (Mask)
        {
            Terminal.INFO("This message has a mask, is this normal?");
            bytes.AddRange(MaskingKey ?? []);
        }
        if (PayloadData != null) //some frames don't have a payload, like close frame and ping/pong frames
            bytes.AddRange(PayloadData);

        return [.. bytes];
    }
    /// <summary>
    /// Set the opcode of the frame
    /// </summary>
    /// <param name="opcode"></param>
    public void SetOpcode(Opcode opcode)
    {
        switch (opcode)
        {
            case Constants.WebSocket.Opcode.CONTINUATION:
                this.Opcode = [false, false, false, false];
                break;
            case Constants.WebSocket.Opcode.TEXT:
                this.Opcode = [false, false, false, true];
                break;
            case Constants.WebSocket.Opcode.BINARY:
                this.Opcode = [false, false, true, false];
                break;
            case Constants.WebSocket.Opcode.CLOSE:
                this.Opcode = [true, false, false, false];
                break;
            case Constants.WebSocket.Opcode.PING:
                this.Opcode = [true, false, false, true];
                break;
            case Constants.WebSocket.Opcode.PONG:
                this.Opcode = [true, false, true, false];
                break;
        }
    }
    /// <summary>
    /// Get frame opcode
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Opcode GetOpcode()
    {
        if (Opcode == null)
            throw new Exception("Frame: opcode == null");
        if (Opcode[0] == false && Opcode[1] == false && Opcode[2] == false && Opcode[3] == false)
            return Constants.WebSocket.Opcode.CONTINUATION;
        if (Opcode[0] == false && Opcode[1] == false && Opcode[2] == false && Opcode[3] == true)
            return Constants.WebSocket.Opcode.TEXT;
        if (Opcode[0] == false && Opcode[1] == false && Opcode[2] == true && Opcode[3] == false)
            return Constants.WebSocket.Opcode.BINARY;
        if (Opcode[0] == true && Opcode[1] == false && Opcode[2] == false && Opcode[3] == false)
            return Constants.WebSocket.Opcode.CLOSE;
        if (Opcode[0] == true && Opcode[1] == false && Opcode[2] == false && Opcode[3] == true)
            return Constants.WebSocket.Opcode.PING;
        if (Opcode[0] == true && Opcode[1] == false && Opcode[2] == true && Opcode[3] == false)
            return Constants.WebSocket.Opcode.PONG;
        throw new Exception("Frame: opcode not recognized");
    }
    public void SetPayload(byte[] payload)
    {
        PayloadData = payload;
        if (payload.Length < 126)
        {
            PayloadLength = Utils.IntTo7Bits(payload.Length);
            return;
        }

        if (payload.Length < 65536)
        {
            PayloadLength = Utils.IntTo7Bits(126);
            ExtendedPayloadLength = BitConverter.GetBytes(payload.Length - 125);
            return;
        }
        //a the moment the extended payload length continued is not supported
    }
    public void SetPayload(string payload)
    {
        SetOpcode(Constants.WebSocket.Opcode.TEXT);
        SetPayload(Encoding.UTF8.GetBytes(payload));
    }

    public override string ToString()
    {
        var sb = "WebSocket Frame:{\n";
        sb += "\tFIN(AL): " + (FIN ? "YES" : "NO") + "\n";
        sb += "\tRSV1: " + (RSV1 ? "✅" : "❌ (Good)") + "\n";
        sb += "\tRSV2: " + (RSV2 ? "✅" : "❌ (Good)") + "\n";
        sb += "\tRSV3: " + (RSV3 ? "✅" : "❌ (Good)") + "\n";
        sb += "\tOpcode: " + (Opcode == null ? "Not set??" : GetOpcode().ToString()) + "\n";
        sb += "\tMask: " + (Mask ? "YES" : "NO") + "\n";
        sb += "\tPayloadLength: " + (PayloadLength == null ? "not set" : PayloadLength.ToInt()) + " bytes\n";
        sb += "\tExtendedPayloadLength: " + (ExtendedPayloadLength == null ? "not set" : BitConverter.ToInt16(ExtendedPayloadLength)) + "\n";
        sb += "\tExtendedPayloadLengthContinued: " + (ExtendedPayloadLengthContinued == null ? "not set" : BitConverter.ToInt64(ExtendedPayloadLengthContinued)) + "\n";
        sb += $"\tMaskingKey: {(MaskingKey == null ? "Not set" : "0x" + BitConverter.ToString(MaskingKey).Replace("-", " 0x"))}\n";
        sb += "\tPayloadData: " + (PayloadData == null ? "Not set??" : "0x" + BitConverter.ToString(PayloadData).Replace("-", " 0x")) + "\n";
        if (Mask)
        {
            sb += "\tUnmaskedPayloadData: " + (PayloadData == null ? "Not set??" : "0x" + BitConverter.ToString(GetPayload()).Replace("-", " 0x")) + "\n";
        }
        sb += "}";
        return sb;
    }
    public byte[] GetPayload()
    {
        if (PayloadData == null) return [];

        //if the frame is masked, unmask the payload
        if (Mask && MaskingKey != null)
        {
            /*
           * src: https://www.rfc-editor.org/rfc/rfc6455.html#section-5.3

           * Octet i of the transformed data ("transformed-octet-i") is the XOR of
           * octet i of the original data ("original-octet-i") with octet at index
           * i modulo 4 of the masking key ("masking-key-octet-j"):

           * j                   = i MOD 4
           * transformed-octet-i = original-octet-i XOR masking-key-octet-j
           */
            //this operation must be done bit level and not byte level

            var payloadBits = new BitArray(PayloadData);
            BitArray maskBits = MaskingKey.Length != PayloadData.Length ? 
                new BitArray(MaskingKey.ExtendRepeating(PayloadData.Length)):
                new BitArray(MaskingKey) ;


            payloadBits = payloadBits.Xor(maskBits);


            byte[] resultBytes = new byte[(payloadBits.Length - 1) / 8 + 1];
            payloadBits.CopyTo(resultBytes, 0);
            return resultBytes;
        }

        return PayloadData;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null) return false;
        

        if (obj is bool[] o)
        {
            bool equal = o[0] == FIN;
            equal = equal && o[1] == RSV1;
            equal = equal && o[2] == RSV2;
            equal = equal && o[3] == RSV3;
            equal = equal && o[4] == Opcode?[0] && o[5] == Opcode?[1] && o[6] == Opcode?[2] && o[7] == Opcode?[3];
            equal = equal && o[8] == Mask;
            //todo add missing checks
            return equal;
        }
        //compare frame
        if (obj is Frame f)
        {
            bool equal = f.GetFIN() == FIN;
            equal = equal && f.GetRSV1() == RSV1;
            equal = equal && f.GetRSV2() == RSV2;
            equal = equal && f.GetRSV3() == RSV3;
            equal = equal && f.GetOpcode() == this.GetOpcode();
            equal = equal && f.GetMask() == Mask;
            equal = equal && f.GetPayloadLength() == PayloadLength;
            equal = equal && f.GetExtendedPayloadLength() == ExtendedPayloadLength;
            equal = equal && f.GetExtendedPayloadLengthContinued() == ExtendedPayloadLengthContinued;
            return equal;

        }
        return false;
    }

    //getters
    public bool GetFIN() => FIN;
    public bool GetRSV1() => RSV1;
    public bool GetRSV2() => RSV2;
    public bool GetRSV3() => RSV3;
    public bool GetMask() => Mask;
    public bool[] GetPayloadLength() => PayloadLength;
    public byte[]? GetExtendedPayloadLength() => ExtendedPayloadLength;
    public byte[]? GetExtendedPayloadLengthContinued() => ExtendedPayloadLengthContinued;

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}