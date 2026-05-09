using HSB.Components;
using HSB.Constants;

namespace HSB.Modules;

[Module(ModuleType.RequestInterceptor, name: "Request Filter", author: "The HSB Team", description:"A simple modules that controls if a request came from a specific IP and allows/blocks it depending on configuration.")]
public class Filter 
{
    [ModuleInvokeMethod]
    public ModuleExitCode Process(Configuration c, ref Request req, ref Response res)
    {
        Terminal.Info(this.ToString());
        var clientIp = req.ClientIp;
        var blockMode = c.BlockMode;
        if (blockMode is BLOCK_MODE.OKLIST or BLOCK_MODE.BANLIST)
        {
            if (!File.Exists("./allowed_ips.txt"))
                return ModuleExitCode.Continue; //no allowed_ips.txt file found, so we allow all requests
        }
        
        
        switch (blockMode)
        {
            case BLOCK_MODE.NONE: return ModuleExitCode.Continue; //no blocking
            case BLOCK_MODE.BANLIST:
                if (c.PermanentIpList.Any(ip => ip == clientIp))
                {
                    return ModuleExitCode.Reject; //blocked request
                }
                var bannedIps = File.ReadAllLines("./banned_ips.txt");
                return bannedIps.Contains(clientIp) ? ModuleExitCode.Reject : ModuleExitCode.Continue; //blocked request
            case BLOCK_MODE.OKLIST:
                if (c.PermanentIpList.Any(ip => ip == clientIp))
                {
                    return ModuleExitCode.Continue; //allowed request
                }

              
                var allowedIps = File.ReadAllLines("./allowed_ips.txt");
                return allowedIps.Contains(clientIp) ? ModuleExitCode.Continue : ModuleExitCode.Reject;
            default:
                return ModuleExitCode.Error;
        }
    }
}