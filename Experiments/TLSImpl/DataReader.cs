namespace HSB;

public class DataReader
{
    private readonly byte[] data = Array.Empty<byte>();
    private uint offset = 0;
    private uint endOffset = 0;


    public DataReader(byte[] data)
    {
        this.data = data;
        endOffset = (uint)data.Length;
    }

    public void Rewind()
    {
        offset = 0;
    }
    public void SetReaderPosition(uint position)
    {
        this.offset = position;
    }

    public void SetEndPosition(uint position)
    {
        endOffset = position;
    }

    public uint GetCurrentPosition()
    {
        return offset;
    }
    public bool DataAvailable()
    {
        return offset < endOffset;
    }

    public int RemainingData => (int)(endOffset - offset);


    public byte ReadByte()
    {
        offset++;
        return data[offset - 1];
    }

    public byte[] ReadBytes(uint size)
    {
        if (size > endOffset - offset)
        {
            size = endOffset - offset - 1;
            Console.WriteLine("Not enough data to read, returning what is available");
        }

        byte[] bytes = new byte[size];
        Array.Copy(data, offset, bytes, 0, size);
        offset += size;
        return bytes;
    }

    public byte[] ReadBytes(uint size, uint offset)
    {
        byte[] bytes = new byte[size];
        Array.Copy(data, offset, bytes, 0, size);
        return bytes;
    }

    public byte[] ReadBytes((uint size, uint offset) values)
    {
        return ReadBytes(values.size, values.offset);
    }
    /// <summary>
    /// Reads 1 byte from the data and returns it as a UInt16
    /// </summary>
    /// <returns></returns>
    public UInt16 ReadSmallUint(){
        byte[] ushrt = ReadBytes(1);
        return Utils.BytesToUShort(new byte[]{0, ushrt[0]});
    }

    //aka uint16
    /// <summary>
    /// Reads 2 bytes from the data and returns them as a ushort
    /// </summary>
    /// <returns></returns>
    public ushort ReadUShort()
    {
        byte[] ushrt = ReadBytes(2);
        return Utils.BytesToUShort(ushrt);
    }
    /// <summary>
    /// Reads 3 bytes from the data and returns them as a uint24
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt24()
    {
        byte[] uint24 = ReadBytes(3);
        return Utils.UInt24ToUInt32(uint24);
    }

    /// <summary>
    /// Reads 4 bytes from the data and returns them as a uint32
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt()
    {
        byte[] ushrt = ReadBytes(4);
        return BitConverter.ToUInt32(ushrt);
    }
    /// <summary>
    /// Reads 4 bytes from the data and returns them as a signed int (int32)
    /// </summary>
    /// <returns></returns>
    public int ReadInt()
    {
        byte[] uin = ReadBytes(4);
        return BitConverter.ToInt32(uin);
    }



}