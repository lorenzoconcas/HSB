using System;
using System.Linq;
using System.Text.RegularExpressions;
using HSB.Constants;

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
    /// <summary>
    /// Check if a path on disk is safe, if not return false and block if autoblock is true
    /// </summary>
    /// <param name="path">The path on disk</param>
    /// <param name="c"></param>
    /// <param name="req"></param>
    /// <param name="res"></param>
    /// <returns></returns>
    
    public static bool SafeRequestOrBan(Configuration c, Request req, Response res)
    {
        if (!PathUtils.IsUnsafePath(req.Url)) return false;

        c.Debug.WARNING($"{req.Method} '{req.Url}' 200 (Requested unsafe path, ignoring request)");
        new Error(res, c, "", HttpCodes.NOT_FOUND).Throw();

        if (!c.IpAutoblock) return true;

        c.Debug.WARNING($"Autoblocking IP {req.ClientIp}");

        if (File.Exists("./banned_ips.txt"))
            File.AppendAllText("./banned_ips.txt", req.ClientIp + "\n");
        else
            File.WriteAllText("./banned_ips.txt", req.ClientIp + "\n");

        return true;
    }
}