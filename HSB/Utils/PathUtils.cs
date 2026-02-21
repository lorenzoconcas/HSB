using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace HSB.Utils;

public static class PathUtils
{
    public static bool IsUnsafePath(string path)
    {
        Regex rgx = new Regex("/(?:^|[\\\\/])\\.\\.(?:[\\\\/]|$)/");
        return rgx.Match(path).Success;
    }

    public static string JoinPath(params string[] parts)
    {
        if (parts.Length == 0)
            return string.Empty;

        return string.Join("/", parts
            .Select((part, index) =>
            {
                if (string.IsNullOrWhiteSpace(part))
                    return string.Empty;

                if (index == 0)
                {
                    // Mantiene lo slash iniziale se presente (es. "/api")
                    return Regex.Replace(part, "/+$", "");
                }
                else
                {
                    // Rimuove slash iniziali e finali per le parti successive
                    return Regex.Replace(part, "^/+|/+$", "");
                }
            })
            .Where(part => part.Length > 0)
        );
    }
}