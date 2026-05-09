using HSB.Components;
using HSB.DefaultPages;
using HSB.Utils;

namespace HSB.Modules;

[Module(ModuleType.RequestInterceptor, name: "File Lister", author: "The HSB Team", description:"This module add the capability to show a a file list ")]
public class FileLister
{
    [ModuleInvokeMethod]
    public ModuleExitCode Run(Configuration c, ref Request req, ref Response res)
    {
        if (!c.GetRawArguments().Contains("--listFiles")) return ModuleExitCode.Continue;
        if (PathUtils.SafeRequestOrBan(c, req, res)) return ModuleExitCode.Reject;
            
        new FileList(req, res, c).Get();
        return ModuleExitCode.Success;
    }
}