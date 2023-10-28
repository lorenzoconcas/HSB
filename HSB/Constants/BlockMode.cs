namespace HSB.Constants;

/// <summary>
/// Defines how to block requests
/// </summary>
public enum BLOCK_MODE
{
    NONE, //no blocking
    OKLIST, //accept only requests from ip presents in allowed_ips.txt
    BANLIST //bans requests from ip presents in banned_ips.txt
}
