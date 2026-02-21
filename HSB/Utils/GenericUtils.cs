using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HSB.Utils;

public static class GenericUtils
{




    public static T Safe<T>(T? o, T safe) => o ?? safe!;

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


}

