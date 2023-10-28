using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HSB;

public static partial class Utils
{

    [GeneratedRegex("/(?:^|[\\\\/])\\.\\.(?:[\\\\/]|$)/")]
    private static partial Regex SafePathRegex();

    public static void PrintLogo()
    {
        Terminal.Write("Welcome to ");
        Terminal.Write("H", BG_COLOR.DEFAULT, FG_COLOR.RED);
        Terminal.Write("S", BG_COLOR.DEFAULT, FG_COLOR.GREEN);
        Terminal.Write("B", BG_COLOR.DEFAULT, FG_COLOR.BLUE);
        Terminal.Write("-", BG_COLOR.DEFAULT, FG_COLOR.WHITE);
        Terminal.Write("#", BG_COLOR.DEFAULT, FG_COLOR.YELLOW);
        Terminal.Write(" (Http Server Boxed)");
        Terminal.WriteLine($" v{Assembly.GetExecutingAssembly().GetName().Version}");
    }
    public static void PrintLoadedAssemblies(bool filter = true)
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assems = currentDomain.GetAssemblies().ToList();
        if (filter)
        {
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
            assems.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));
        }

        foreach (Assembly assem in assems)
        {
            List<Type> classes = assem.GetTypes().ToList();

            foreach (var c in classes)
                Terminal.WriteLine(c.FullName, BG_COLOR.WHITE, FG_COLOR.BLUE);
        }
    }
    public static T Safe<T>(T? o, T safe) => o ?? safe!;
    public static T TryGetValueFromDict<T>(this Dictionary<string, T> dict, string key, T safe)
    {
        if (dict.ContainsKey(key)) return dict[key];
        else return safe;
    }
    public static string DictToString(this Dictionary<string, string> obj)
    {
        string s = "";
        foreach (var v in obj)
        {
            s += v.Key + " - " + v.Value + "\n";
        }
        return s;
    }
    internal static string LoadResourceAsString(string resName)
    {
        var assembly = Assembly.GetCallingAssembly();
        string resourceName;

        try
        {

            resourceName = assembly.GetManifestResourceNames().First(str => str.EndsWith(resName));
        }
        catch (Exception)
        {
            return "";
        }


        string result;

        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            return "";
        }

        using (StreamReader reader = new(stream))
        {
            result = reader.ReadToEnd();
        }
        return result;
    }
    //source:https://stackoverflow.com/questions/3825390/effective-way-to-find-any-files-encoding
    /// <summary>
    /// Determines a text file's encoding by analyzing its byte order mark (BOM).
    /// Defaults to ASCII when detection of the text file's endianness fails.
    /// </summary>
    /// <param name="filename">The text file to analyze.</param>
    /// <returns>The detected encoding.</returns>
    public static Encoding GetEncoding(string filename)
    {
        // Read the BOM
        var bom = new byte[4];
        using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
        {
            file.Read(bom, 0, 4);
        }

        return GetEncoding(bom);

    }
    public static Encoding GetEncoding(byte[] file)
    {
        var bom = new byte[4];
        bom[0] = file[0];
        bom[1] = file[1];
        bom[2] = file[2];
        bom[3] = file[3];

        // Analyze the BOM

        // if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
        if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
        if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Encoding.UTF32; //UTF-32LE
        if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
        if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
        if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return new UTF32Encoding(true, true);  //UTF-32BE



        // We actually have no idea what the encoding is if we reach this point, so
        // you may wish to return null instead of defaulting to ASCII
        return Encoding.ASCII;
    }
    public static string GenerateRandomString(int size, bool onlyUpperCase = false, bool useNumbers = true, bool useSpecialCharacters = true)
    {

        Random random = new();
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        if (!onlyUpperCase)
            chars += "abcdefghijklmnopqrstuvwxyz";

        if (useNumbers)
            chars += "0123456789";

        if (useSpecialCharacters)
            chars += "!@#$%^&*()_+-=[]{}|;:,.<>?";


        string rndStr = "";
        for (int i = 0; i < size; i++)
        {
            rndStr += chars[random.Next(chars.Length)];
        }

        return rndStr;
    }
    public static T[] SubArray<T>(this T[] array, int offset, int length)
    {
        T[] result = new T[length];
        Array.Copy(array, offset, result, 0, length);
        return result;
    }
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
    private static int[] ComputeFailure(byte[] sequence)
    {
        int[] failure = new int[sequence.Length];
        int j = 0;
        for (int i = 1; i < sequence.Length; i++)
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
        List<byte[]> result = new();
        int offset = 0;
        while (offset < array.Length)
        {
            int index = IndexOf(array[offset..], separator);
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

    public static string AsSizeHumanReadable(this int size)
    {
        const int BASE = 1024;
        const int KB = 1_048_576;
        const int MB = 1_073_741_824;


        if (size < BASE)
        {
            return size + "B";
        }
        else if (size < KB)
        {
            return (size / BASE) + "KB";
        }
        else if (size < MB)
        {
            return (size / KB) + "MB";
        }
        else
        {
            return (size / MB) + "GB";
        }
    }

    public static bool IsUnsafePath(string path)
    {
        Regex rgx = SafePathRegex();
        return rgx.Match(path).Success;
    }
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
        List<bool> bits = new();

        foreach (var b in array)
        {
            bits.AddRange(b.ToBitArray());
        }
        return bits.ToArray();
    }

    public static byte GetByte(params bool[] bits)
    {
        if (bits.Length > 8) throw new ArgumentException("The array must have 8 elements");
        //todo Check platform endianess
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

        List<byte> bytes = new();
        for (int i = 0; i < bits.Length; i += 8)
        {
            bytes.Add(GetByte(bits[i..(i + 8)]));
        }
        return bytes.ToArray();
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
        List<T> result = new();
        for (int i = 0; i < size; i++)
        {
            result.Add(array[i % array.Length]);
        }
        return result.ToArray();

    }

    internal static bool IsEmbeddedResource(string url)
    {
        if (url.StartsWith("/")) url = url[1..];
        url = url.Replace("/", "."); 

        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = currentDomain.GetAssemblies().ToList();

        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));

        foreach (var assembly in assemblies)
        {        
            foreach (var res in assembly.GetManifestResourceNames())
            {
                if (res.EndsWith(url)) return true;
            }
        }
        return false;
    }

    //Search all the loaded assemblies for the resource, then reads it and returns it as object
    internal static T LoadResource<T>(string resName)
    {
        if (resName.StartsWith("/")) resName = resName[1..];
        resName = resName.Replace("/", "."); 
        AppDomain currentDomain = AppDomain.CurrentDomain;
        List<Assembly> assemblies = currentDomain.GetAssemblies().ToList();

        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("System"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Microsoft"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("Internal"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("ILLink"));
        assemblies.RemoveAll(a => a.ManifestModule.Name.StartsWith("FxResources"));

        foreach (var assembly in assemblies)
        {
            string resourceName = assembly.GetManifestResourceNames().First(str => str.EndsWith(resName));
            if (resourceName != null)
            {
                using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
                using StreamReader reader = new(stream);
                return (T)Convert.ChangeType(reader.ReadToEnd(), typeof(T));
            }
        }
        return default!;
    }
}

