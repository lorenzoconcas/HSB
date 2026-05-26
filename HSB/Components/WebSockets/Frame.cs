using System.Collections;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using HSB.Constants.WebSocket;
using HSB.Utils;

namespace HSB.Components.WebSockets;

public sealed class Frame
{
    private bool fin;
    private bool rsv1;
    private bool rsv2;
    private bool rsv3;
    private Opcode opcode;
    private bool mask;
    private byte[]? maskingKey;
    private byte[] payloadData;

    public Frame(
        bool fin = true,
        bool rsv1 = false,
        bool rsv2 = false,
        bool rsv3 = false,
        Opcode opcode = Opcode.TEXT,
        bool mask = false)
    {
        this.fin = fin;
        this.rsv1 = rsv1;
        this.rsv2 = rsv2;
        this.rsv3 = rsv3;
        this.opcode = opcode;
        this.mask = mask;
        payloadData = [];
    }

    public Frame(byte[] data)
    {
        if (!TryRead(data, int.MaxValue, out var parsedFrame, out var consumed, out var error) ||
            parsedFrame == null ||
            consumed != data.Length)
        {
            throw new InvalidOperationException(error ?? "Incomplete WebSocket frame");
        }

        fin = parsedFrame.fin;
        rsv1 = parsedFrame.rsv1;
        rsv2 = parsedFrame.rsv2;
        rsv3 = parsedFrame.rsv3;
        opcode = parsedFrame.opcode;
        mask = parsedFrame.mask;
        maskingKey = parsedFrame.maskingKey == null ? null : [.. parsedFrame.maskingKey];
        payloadData = [.. parsedFrame.payloadData];
    }

    public static bool TryRead(
        ReadOnlySpan<byte> data,
        int maxPayloadBytes,
        out Frame? frame,
        out int consumed,
        out string? error)
    {
        frame = null;
        consumed = 0;
        error = null;

        if (data.Length < 2)
        {
            return false;
        }

        var firstByte = data[0];
        var secondByte = data[1];

        var fin = (firstByte & 0b1000_0000) != 0;
        var rsv1 = (firstByte & 0b0100_0000) != 0;
        var rsv2 = (firstByte & 0b0010_0000) != 0;
        var rsv3 = (firstByte & 0b0001_0000) != 0;

        if (rsv1 || rsv2 || rsv3)
        {
            error = "RSV bits are not supported";
            return false;
        }

        if (!TryDecodeOpcode(firstByte & 0b0000_1111, out var opcode))
        {
            error = "Frame opcode not recognized";
            return false;
        }

        var mask = (secondByte & 0b1000_0000) != 0;
        ulong payloadLength = (ulong)(secondByte & 0b0111_1111);
        var offset = 2;

        if (payloadLength == 126)
        {
            if (data.Length < offset + 2)
            {
                return false;
            }

            payloadLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            offset += 2;
        }
        else if (payloadLength == 127)
        {
            if (data.Length < offset + 8)
            {
                return false;
            }

            payloadLength = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
            offset += 8;
        }

        if (payloadLength > int.MaxValue)
        {
            error = "Frame payload exceeds supported size";
            return false;
        }

        if (payloadLength > (ulong)Math.Max(0, maxPayloadBytes))
        {
            error = "Frame payload exceeds configured limit";
            return false;
        }

        if (IsControlOpcode(opcode) && (!fin || payloadLength > 125))
        {
            error = "Control frame is malformed";
            return false;
        }

        byte[]? maskingKey = null;
        if (mask)
        {
            if (data.Length < offset + 4)
            {
                return false;
            }

            maskingKey = data.Slice(offset, 4).ToArray();
            offset += 4;
        }

        var totalLength = offset + (int)payloadLength;
        if (data.Length < totalLength)
        {
            return false;
        }

        var payload = data.Slice(offset, (int)payloadLength).ToArray();
        if (mask && maskingKey != null)
        {
            ApplyMask(payload, maskingKey);
        }

        frame = new Frame(fin, rsv1, rsv2, rsv3, opcode, mask)
        {
            maskingKey = maskingKey,
            payloadData = payload
        };
        consumed = totalLength;
        return true;
    }

    public byte[] Build()
    {
        var header = new List<byte>(14 + payloadData.Length);
        header.Add((byte)(
            (fin ? 0b1000_0000 : 0) |
            (rsv1 ? 0b0100_0000 : 0) |
            (rsv2 ? 0b0010_0000 : 0) |
            (rsv3 ? 0b0001_0000 : 0) |
            EncodeOpcode(opcode)));

        if (mask && maskingKey == null)
        {
            maskingKey = RandomNumberGenerator.GetBytes(4);
        }

        var payload = mask && maskingKey != null ? [.. payloadData] : payloadData;
        if (mask && maskingKey != null)
        {
            ApplyMask(payload, maskingKey);
        }

        if (payload.LongLength <= 125)
        {
            header.Add((byte)((mask ? 0b1000_0000 : 0) | (byte)payload.Length));
        }
        else if (payload.LongLength <= ushort.MaxValue)
        {
            header.Add((byte)((mask ? 0b1000_0000 : 0) | 126));
            Span<byte> extendedLength = stackalloc byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(extendedLength, (ushort)payload.Length);
            header.AddRange(extendedLength.ToArray());
        }
        else
        {
            header.Add((byte)((mask ? 0b1000_0000 : 0) | 127));
            Span<byte> extendedLength = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(extendedLength, (ulong)payload.LongLength);
            header.AddRange(extendedLength.ToArray());
        }

        if (mask && maskingKey != null)
        {
            header.AddRange(maskingKey);
        }

        header.AddRange(payload);
        return [.. header];
    }

    public void SetOpcode(Opcode newOpcode)
    {
        opcode = newOpcode;
    }

    public Opcode GetOpcode()
    {
        return opcode;
    }

    public void SetPayload(byte[] payload)
    {
        payloadData = payload ?? [];
    }

    public void SetPayload(string payload)
    {
        SetOpcode(Opcode.TEXT);
        SetPayload(Encoding.UTF8.GetBytes(payload));
    }

    public byte[] GetPayload()
    {
        return [.. payloadData];
    }

    public override string ToString()
    {
        var sb = "WebSocket Frame:{\n";
        sb += "\tFIN(AL): " + (fin ? "YES" : "NO") + "\n";
        sb += "\tRSV1: " + (rsv1 ? "YES" : "NO") + "\n";
        sb += "\tRSV2: " + (rsv2 ? "YES" : "NO") + "\n";
        sb += "\tRSV3: " + (rsv3 ? "YES" : "NO") + "\n";
        sb += "\tOpcode: " + opcode + "\n";
        sb += "\tMask: " + (mask ? "YES" : "NO") + "\n";
        sb += "\tPayloadLength: " + payloadData.Length + " bytes\n";
        sb += $"\tMaskingKey: {(maskingKey == null ? "Not set" : "0x" + BitConverter.ToString(maskingKey).Replace("-", " 0x"))}\n";
        sb += "\tPayloadData: " + (payloadData.Length == 0 ? "Not set" : "0x" + BitConverter.ToString(payloadData).Replace("-", " 0x")) + "\n";
        sb += "}";
        return sb;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Frame other)
        {
            return false;
        }

        return other.fin == fin &&
               other.rsv1 == rsv1 &&
               other.rsv2 == rsv2 &&
               other.rsv3 == rsv3 &&
               other.opcode == opcode &&
               other.mask == mask &&
               ((other.maskingKey == null && maskingKey == null) ||
                (other.maskingKey != null && maskingKey != null && other.maskingKey.SequenceEqual(maskingKey))) &&
               other.payloadData.SequenceEqual(payloadData);
    }

    public bool GetFIN() => fin;
    public bool GetRSV1() => rsv1;
    public bool GetRSV2() => rsv2;
    public bool GetRSV3() => rsv3;
    public bool GetMask() => mask;
    public bool[] GetPayloadLength() => ByteUtils.IntTo7Bits(GetPayloadLengthValue());
    public byte[]? GetExtendedPayloadLength()
    {
        if (payloadData.Length <= 125 || payloadData.Length > ushort.MaxValue)
        {
            return null;
        }

        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)payloadData.Length);
        return bytes.ToArray();
    }

    public byte[]? GetExtendedPayloadLengthContinued()
    {
        if (payloadData.Length <= ushort.MaxValue)
        {
            return null;
        }

        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, (ulong)payloadData.Length);
        return bytes.ToArray();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(fin, rsv1, rsv2, rsv3, opcode, mask, payloadData.Length);
    }

    public void Dispose()
    {
        fin = false;
        rsv1 = false;
        rsv2 = false;
        rsv3 = false;
        opcode = Opcode.TEXT;
        mask = false;
        maskingKey = null;
        payloadData = [];
    }

    private int GetPayloadLengthValue()
    {
        if (payloadData.Length <= 125)
        {
            return payloadData.Length;
        }

        return payloadData.Length <= ushort.MaxValue ? 126 : 127;
    }

    private static bool TryDecodeOpcode(int rawOpcode, out Opcode opcode)
    {
        switch (rawOpcode)
        {
            case 0x0:
                opcode = Opcode.CONTINUATION;
                return true;
            case 0x1:
                opcode = Opcode.TEXT;
                return true;
            case 0x2:
                opcode = Opcode.BINARY;
                return true;
            case 0x8:
                opcode = Opcode.CLOSE;
                return true;
            case 0x9:
                opcode = Opcode.PING;
                return true;
            case 0xA:
                opcode = Opcode.PONG;
                return true;
            default:
                opcode = default;
                return false;
        }
    }

    private static int EncodeOpcode(Opcode opcode)
    {
        return opcode switch
        {
            Opcode.CONTINUATION => 0x0,
            Opcode.TEXT => 0x1,
            Opcode.BINARY => 0x2,
            Opcode.CLOSE => 0x8,
            Opcode.PING => 0x9,
            Opcode.PONG => 0xA,
            _ => throw new InvalidOperationException("Unsupported WebSocket opcode")
        };
    }

    private static bool IsControlOpcode(Opcode opcode)
    {
        return opcode is Opcode.CLOSE or Opcode.PING or Opcode.PONG;
    }

    private static void ApplyMask(byte[] payload, byte[] key)
    {
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] ^= key[i % key.Length];
        }
    }
}
