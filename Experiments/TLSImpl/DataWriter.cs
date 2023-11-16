namespace HSB;

/// <summary>
/// A class to simplify the construction of byte arrays
/// </summary>
public class DataWriter{
    private readonly List<byte> data;

    public DataWriter(){
        data = [];
    }
    /// <summary>
    /// Append a byte to the byte array
    /// </summary>
    /// <param name="b"></param>
    public void Append(Byte b){
        data.Add(b);
    }
    /// <summary>
    /// Append a byte array to the byte array
    /// </summary>
    /// <param name="b"></param>
    public void Append(byte[] b){
        data.AddRange(b);
    }
    /// <summary>
    /// Append a ushort (aka uint16) to the byte array
    /// </summary>
    /// <param name="s">Ushort (aka uint16)</param>
    public void Append(ushort s){
        data.AddRange(BitConverter.GetBytes(s));
    }
    /// <summary>
    /// Append a uint (aka uint32) to the byte array
    /// </summary>
    /// <param name="i"></param>
    public void Append(uint i){
        data.AddRange(BitConverter.GetBytes(i));
    }
    /// <summary>
    /// Append a ulong (aka uint64) to the byte array
    /// </summary>
    public void Append(ulong l){
        data.AddRange(BitConverter.GetBytes(l));
    }
    /// <summary>
    /// Insert a byte at a specific position in the byte array
    /// </summary>
    public void Insert(int position, byte b){
        data.Insert(position, b);
    }
    /// <summary>
    /// Insert a byte array at a specific position in the byte array
    /// </summary>
    public void Insert(int position, byte[] b){
        data.InsertRange(position, b);
    }
    /// <summary>
    /// Gets the byte at a specific position in the byte array
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public byte GetByte(int position){
        return data[position];
    }
    /// <summary>
    /// Get a byte array from a specific position in the byte array
    public byte[] GetBytes(int position, int size){
        byte[] bytes = new byte[size];
        Array.Copy(data.ToArray(), position, bytes, 0, size);
        return bytes;
    }
    /// <summary>
    /// Build the byte array
    /// </summary>
    public byte[] Build(){
        return [.. data];
    }

    
}