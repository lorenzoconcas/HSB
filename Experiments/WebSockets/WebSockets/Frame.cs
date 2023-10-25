using System.IO.Compression;
using Microsoft.VisualBasic;

namespace HSB.Components.WebSockets;

public class Frame
{

    private bool FIN { get; set; }
    private bool RSV1 { get; set; }
    private bool RSV2 { get; set; }
    private bool RSV3 { get; set; }
    private bool[]? opcode { get; set; }
    private bool Mask { get; set; }
    private bool[]? PayloadLength;
    private bool[]? ExtendedPayloadLength { get; set; }
    private bool[]? ExtendedPayloadLengthContinued { get; set; }
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
    public Frame(bool fin = true, bool rsv1 = false, bool rsv2 = false, bool rsv3 = false, bool[]? opcode = null, bool mask = false)
    {
        FIN = fin;
        RSV1 = rsv1;
        RSV2 = rsv2;
        RSV3 = rsv3;
        this.opcode = opcode;
        Mask = mask;
        /*  PayloadLength = payloadLength;
         ExtendedPayloadLength = extendedPayloadLength;
         ExtendedPayloadLengthContinued = extendedPayloadLengthContinued;
         MaskingKey = maskingKey;
         MaskingKeyContinued = maskinKeyContinued;
         PayloadData = payloadData;*/
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
        opcode = first8bits[4..];//new bool[] { first8bits[4], first8bits[5], first8bits[6], first8bits[7] };
        var maskAndPayloadLength = data[1].ToBitArray();
        Mask = maskAndPayloadLength[0];
        PayloadLength = maskAndPayloadLength[1..];
        //print Payload lenght in binary
        Terminal.INFO("Payload length (bin): " + string.Join("", PayloadLength.Select(x => x ? 1 : 0)));


        if (PayloadLength.ToInt() == 126)
        {
            //ExtendedPayloadLength is present
            if (data.Length < 4)
                throw new Exception("Frame: data.Length < 4 and Payload length is 126");
            ExtendedPayloadLength = new bool[] {
                data[2].ToBitArray()[0],
                data[2].ToBitArray()[1],
                data[2].ToBitArray()[2],
                data[2].ToBitArray()[3],
                data[2].ToBitArray()[4],
                data[2].ToBitArray()[5],
                data[2].ToBitArray()[6],
                data[2].ToBitArray()[7]
                 };
        }
        else if (PayloadLength.ToInt() == 127)
        {
            //ExtendedPayloadLengthContinued is present
            if (data.Length < 10)
                throw new Exception("Frame: data.Length < 10 and Payload length is 127");
            ExtendedPayloadLengthContinued = new bool[] {
                data[2].ToBitArray()[0],
                data[2].ToBitArray()[1],
                data[2].ToBitArray()[2],
                data[2].ToBitArray()[3],
                data[2].ToBitArray()[4],
                data[2].ToBitArray()[5],
                data[2].ToBitArray()[6],
                data[2].ToBitArray()[7],
                data[3].ToBitArray()[0],
                data[3].ToBitArray()[1],
                data[3].ToBitArray()[2],
                data[3].ToBitArray()[3],
                data[3].ToBitArray()[4],
                data[3].ToBitArray()[5],
                data[3].ToBitArray()[6],
                data[3].ToBitArray()[7]
            };
        }

        //masking key
        int position = 2;
        if (Mask)
        {
            //MaskingKey is 0 or 4 bytes
            //position dependes on the length of the payload
            //if payload.length < 126 -> position = 2
            //if payload.length == 126 -> payload extend size is 2 bytes (16 bits) -> position = 4
            //if payload.length == 127 -> payload extend size is 8 bytes (64 bits) -> position = 10
            if (ExtendedPayloadLength != null)
                position = 4;
            if (ExtendedPayloadLengthContinued != null)
                position = 10;
            if (data.Length < position + 4)
                throw new Exception($"Frame: data.Length < {position} + 4 and a mask is present");

            MaskingKey = data[position..(position + 4)];

            /*
            * src: https://www.rfc-editor.org/rfc/rfc6455.html#section-5.3
            
            * Octet i of the transformed data ("transformed-octet-i") is the XOR of
            * octet i of the original data ("original-octet-i") with octet at index
            * i modulo 4 of the masking key ("masking-key-octet-j"):

            * j                   = i MOD 4
            * transformed-octet-i = original-octet-i XOR masking-key-octet-j

             */

            PayloadData = new byte[data[position..].Length];

            for (int i = 0; i < data[position..].Length; i++)
            {
                PayloadData[i] = (byte)(PayloadData[i] ^ MaskingKey[i % 4]);
            }
        }
        else
        {
            PayloadData = data[2..];
        }
    }
    /// <summary>
    /// Build the frame
    /// </summary>
    /// <returns></returns>
    public byte[] Build()
    {
        List<byte> bytes = new(){
            Utils.GetByte(
                FIN,
                RSV1,
                RSV2,
                RSV3,
                opcode?[0] ?? false,
                opcode?[1] ?? false,
                opcode?[2] ?? false,
                opcode?[3] ?? false
            ),
            Utils.GetByte(
                //mask and payload length
                Mask,
                PayloadLength?[0] ?? false,
                PayloadLength?[1] ?? false,
                PayloadLength?[2] ?? false,
                PayloadLength?[3] ?? false,
                PayloadLength?[4] ?? false,
                PayloadLength?[5] ?? false,
                PayloadLength?[6] ?? false
            )
        };
        //todo add missing
        return bytes.ToArray();
    }
    /// <summary>
    /// Set the opcode of the frame
    /// </summary>
    /// <param name="opcode"></param>
    public void SetOpcode(Opcode opcode)
    {
        switch (opcode)
        {
            case Opcode.CONTINUATION:
                this.opcode = new bool[] { false, false, false, false };
                break;
            case Opcode.TEXT:
                this.opcode = new bool[] { false, false, false, true };
                break;
            case Opcode.BINARY:
                this.opcode = new bool[] { false, false, true, false };
                break;
            case Opcode.CLOSE:
                this.opcode = new bool[] { true, false, false, false };
                break;
            case Opcode.PING:
                this.opcode = new bool[] { true, false, false, true };
                break;
            case Opcode.PONG:
                this.opcode = new bool[] { true, false, true, false };
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
        if (opcode == null)
            throw new Exception("Frame: opcode == null");
        if (opcode[0] == false && opcode[1] == false && opcode[2] == false && opcode[3] == false)
            return Opcode.CONTINUATION;
        if (opcode[0] == false && opcode[1] == false && opcode[2] == false && opcode[3] == true)
            return Opcode.TEXT;
        if (opcode[0] == false && opcode[1] == false && opcode[2] == true && opcode[3] == false)
            return Opcode.BINARY;
        if (opcode[0] == true && opcode[1] == false && opcode[2] == false && opcode[3] == false)
            return Opcode.CLOSE;
        if (opcode[0] == true && opcode[1] == false && opcode[2] == false && opcode[3] == true)
            return Opcode.PING;
        if (opcode[0] == true && opcode[1] == false && opcode[2] == true && opcode[3] == false)
            return Opcode.PONG;
        throw new Exception("Frame: opcode not recognized");
    }
    public void SetPayload(byte[] payload)
    {
        PayloadData = payload;
        if (payload.Length < 126)
            PayloadLength = Utils.IntTo7Bits(payload.Length);
        else if (payload.Length < 65536)
            ExtendedPayloadLength = Utils.IntTo7Bits(payload.Length);
        else
            ExtendedPayloadLengthContinued = Utils.IntTo7Bits(payload.Length);

    }

    public override string ToString()
    {
        var sb = "WebSocket Frame:{\n";
        sb += "\tFIN(AL): " + (FIN ? "YES" : "NO") + "\n";
        sb += "\tRSV1: " + (RSV1 ? "✅" : "❌") + "\n";
        sb += "\tRSV2: " + (RSV2 ? "✅" : "❌") + "\n";
        sb += "\tRSV3: " + (RSV3 ? "✅" : "❌") + "\n";
        sb += "\tOpcode: " + (opcode == null ? "not set??" : GetOpcode().ToString()) + "\n";
        sb += "\tMask: " + (Mask ? "YES" : "NO") + "\n";
        sb += "\tPayloadLength: " + (PayloadLength == null ? "not set" : PayloadLength.ToInt()) + " bytes\n";
        sb += "\tExtendedPayloadLength: " + (ExtendedPayloadLength == null ? "not set" : ExtendedPayloadLength.ToInt()) + "\n";
        sb += "\tExtendedPayloadLengthContinued: " + (ExtendedPayloadLengthContinued == null ? "not set" : ExtendedPayloadLengthContinued.ToInt()) + "\n";
        sb += $"\tMaskingKey: {(MaskingKey == null ? "not set" : "0x"+BitConverter.ToString(MaskingKey).Replace("-", " 0x"))}\n";
        sb += "\tPayloadData: " + (PayloadData == null ? "not set??" : "0x"+BitConverter.ToString(PayloadData).Replace("-", " 0x")) + "\n";
        sb += "}";
        return sb;
    }
    public byte[] GetPayload()
    {
        if(PayloadData == null) return Array.Empty<byte>();
        if(PayloadData.Length < PayloadLength.ToInt())
         {
            //fill difference with 0
            var diff = PayloadLength.ToInt() - PayloadData.Length;
            var tmp = new byte[PayloadLength.ToInt()];
            for (int i = 0; i < PayloadData.Length; i++)
            {
                tmp[i] = PayloadData[i];
            }
            PayloadData = tmp;
         }
        return PayloadData;
    }
}