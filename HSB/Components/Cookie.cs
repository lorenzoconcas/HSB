// 
// lore
using HSB.Exceptions;

namespace HSB;

public class Cookie
{
    public enum CookiePriority { LOW, MEDIUM, HIGH };
    public enum SameSite { Lax, Strict, None };
    public string name;
    public string value;
    public DateTime? expiration;
    public string? path;
    public CookiePriority? priority;
    public bool? secure;
    public bool? HttpOnly;
    public SameSite? sameSite;

    public Cookie()
    {
        name = "";
        value = "";
        expiration = null;
        path = null;
        priority = null;
    }

    public Cookie(string name, string value)
    {
        this.name = name;
        this.value = value;
    }

    public Cookie(string name, string value, DateTime? expires, string? path, CookiePriority? priority)
    {
        this.name = name;
        this.value = value;
        expiration = expires;
        this.path = path;
        this.priority = priority;
    }

    public Cookie(string cookieContent)
    {
        var s = cookieContent.Split("=");
        name = s[0];
        value = s[1];
    }

    public override string ToString()
    {
        if (name == "" && value == "")
        {
            throw new CookieValuesNotSetException();
        }

        string cookie = $"{name}={value}";

        if (expiration.HasValue)
        {
            cookie += $"; Expires={expiration.Value:ddd, dd MMM yyyy HH:mm:ss 'GMT'}";
        }

        if (path != null)
            cookie += $"; Path={path}";

        if (priority.HasValue)
            cookie += $"; Priority={PriorityToString(priority.Value)}";

        if (secure != null)
            cookie += "; Secure";

        if (HttpOnly != null)
            cookie += "; HttpOnly";

        if (sameSite.HasValue)
            cookie += $"; SameSite={SameSiteToString(sameSite.Value)}";

        return cookie;
    }

    private static string PriorityToString(CookiePriority priority) => priority switch
    {
        CookiePriority.LOW => "Low",
        CookiePriority.MEDIUM => "Medium",
        CookiePriority.HIGH => "High",
        _ => throw new NotImplementedException()
    };

    private static string SameSiteToString(SameSite sameSite) => sameSite switch
    {
        SameSite.Lax => "Lax",
        SameSite.Strict => "Strict",
        SameSite.None => "None",
        _ => throw new NotImplementedException(),
    };
}

