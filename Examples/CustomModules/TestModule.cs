using HSB.Components;

namespace Modules;
using HSB;
[Module(ModuleType.RequestInterceptor, name: "Test")]
class TestModule
{
    [ModuleInvokeMethod]
    public ModuleExitCode test()
    {
        Terminal.Info(this.ToString());
        return ModuleExitCode.Continue;
    }
}