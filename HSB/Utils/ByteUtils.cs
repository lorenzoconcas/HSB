namespace HSB.Utils;

public static class ByteUtils
{
    public static bool[] IntTo7Bits(int value)
    {
        bool[] bits = new bool[7];
        for (int i = 0; i < 7; i++)
        {
            bits[i] = (value & (1 << i)) != 0;
        }
        return bits.Reverse().ToArray();
    }
    
    
    /// <summary>
    /// Convert an int to a 7 bit array, useful for WebSockets frames
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool[] To7BitArray(this int value)
    {
        bool[] bits = new bool[7];
        for (int i = 0; i < 7; i++)
        {
            bits[i] = (value & (1 << i)) != 0;
        }
        return bits;
    }
    /// <summary>
    /// Convert a 7 bit array to an int, useful for WebSockets frames
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    public static int ToInt(this bool[] bits)
    {
        if (bits.Length != 7) throw new ArgumentException("The array must have 7 elements");
        int value = 0;
        //check endianness!!
        bits = bits.Reverse().ToArray();
        for (int i = 0; i < 7; i++)
            if (bits[i])
                value += (int)Math.Pow(2, i);

        return value;
    }

    public static bool[] ToBitArray(this byte value)
    {
        bool[] bits = new bool[8];
        for (int i = 0; i < 8; i++)
        {
            bits[i] = (value & (1 << i)) != 0;
        }
        return bits.Reverse().ToArray();
    }

    public static bool[] ToBitArray(this byte[] array)
    {
        List<bool> bits = [];

        foreach (var b in array)
        {
            bits.AddRange(b.ToBitArray());
        }
        return [.. bits];
    }

    public static byte GetByte(params bool[] bits)
    {
        if (bits.Length > 8) throw new ArgumentException("The array must have 8 elements");
        //todo Check platform endianness
        bits = bits.Reverse().ToArray();
        byte value = 0;
        for (int i = 0; i < 8; i++)
        {
            if (bits[i])
                value += (byte)Math.Pow(2, i);
        }
        return value;

    }


    /// <summary>
    /// Converts an int16 to a 16 bit array
    /// </summary>
    /// <param name="length"></param>
    /// <returns></returns>
    public static bool[] IntTo16Bits(int length)
    {
        bool[] bits = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bits[i] = (length & (1 << i)) != 0;
        }
        return bits;
    }
    /// <summary>
    /// Converts an int32 to a 64 bit array (8 bytes)
    /// </summary>
    /// <param name="lenght"></param>
    /// <returns></returns>
    public static bool[] Int64To64Bits(int lenght)
    {
        bool[] bits = new bool[64];
        for (int i = 0; i < 64; i++)
        {
            bits[i] = (lenght & (1 << i)) != 0;
        }
        return bits;
    }

    public static byte[] ToByteArray(this bool[] bits)
    {
        if (bits.Length % 8 != 0) throw new ArgumentException("The array must have a length multiple of 8");

        List<byte> bytes = [];
        for (int i = 0; i < bits.Length; i += 8)
        {
            bytes.Add(GetByte(bits[i..(i + 8)]));
        }
        return [.. bytes];
    }
    
    
}