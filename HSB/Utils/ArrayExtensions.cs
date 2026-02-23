namespace HSB.Utils;

public static class ArrayExtensions
{
    private static int[] ComputeFailure(byte[] sequence)
    {
        var failure = new int[sequence.Length];
        var j = 0;
        for (var i = 1; i < sequence.Length; i++)
        {
            while (j > 0 && sequence[j] != sequence[i])
            {
                j = failure[j - 1];
            }
            if (sequence[j] == sequence[i])
            {
                j++;
            }
            failure[i] = j;
        }
        return failure;
    }
    
    /// <summary>
    /// This function works like Split for the string, but for byte arrays
    /// </summary>
    /// <param name="array"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static List<byte[]> Split(this byte[] array, byte[] separator)
    {
        List<byte[]> result = [];
        var offset = 0;
        while (offset < array.Length)
        {
            var index = array[offset..].IndexOf(separator);
            if (index == -1)
            {
                result.Add(array[offset..]);
                break;
            }
            result.Add(array[offset..(offset + index)]);
            offset += index + separator.Length;
        }
        return result;
    }
    
    /// <summary>
    /// Return a subarray of the given array starting from the offset and with the specified length
    /// </summary>
    /// <param name="array"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T[] SubArray<T>(this T[] array, int offset, int length)
    {
        T[] result = new T[length];
        Array.Copy(array, offset, result, 0, length);
        return result;
    }
    /// <summary>
    /// Search a given byte sequence in the given byte array and return the index of the first occurrence, or -1 if not found
    /// </summary>
    /// <param name="data"></param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    public static int IndexOf(this byte[] data, byte[] sequence)
    {
        //KMP algorithm
        int[] failure = ComputeFailure(sequence);
        int j = 0;
        if (data.Length == 0) return -1;
        for (int i = 0; i < data.Length; i++)
        {
            while (j > 0 && sequence[j] != data[i])
            {
                j = failure[j - 1];
            }
            if (sequence[j] == data[i])
            {
                j++;
            }
            if (j == sequence.Length)
            {
                return i - sequence.Length + 1;
            }
        }
        return -1;
    }

    /// <summary>
    /// Extends (or reduces) an array repeating it until it reaches the specified size
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="array"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    public static T[] ExtendRepeating<T>(this T[] array, int size)
    {
        List<T> result = [];
        for (int i = 0; i < size; i++)
        {
            result.Add(array[i % array.Length]);
        }
        return [.. result];

    }
}