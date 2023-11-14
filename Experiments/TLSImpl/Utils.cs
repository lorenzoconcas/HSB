namespace HSB;
//todo -> merge with HSB/Utils.cs
public static class Utils
{
    public static uint UInt24ToUInt32(byte[] data)
    {
        if (data.Length != 3)
            throw new Exception("UInt24ToUInt32: data.Length != 3");
        return (uint)((data[0] << 16) | (data[1] << 8) | data[2]);
    }

    public static uint UInt24ToUInt32(byte byte1, byte byte2, byte byte3)
    {
        return (uint)((byte1 << 16) | (byte2 << 8) | byte3);
    }

    public static ushort BytesToUShort(byte[] data)
    {
        if (data.Length != 2)
            throw new Exception("BytesToUShort: data.Length != 2");
        return (ushort)((data[0] << 8) | data[1]);
    }

    public static ushort BytesToUShort(byte byte1, byte byte2)
    {
        return (ushort)((byte1 << 8) | byte2);
    }

    public static byte[] UShortToBytes(ushort data)
    {
        return new byte[] { (byte)(data >> 8), (byte)(data & 0xFF) };
    }

}
