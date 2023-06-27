using System;
using HSB;
namespace TestRunner
{
    public class HSBTestRunner
    {
        private static void Main(string[] args)
        {
            Terminal.INFO("HELLO");
            Configuration c = new()
            {
                address = "127.0.0.1",
                port = 8080
            };
            //  Terminal.INFO("Printing loaded assemblies (filtered)");
            // Utils.printLoadedAssemblies();
            _ = new Server(c);
        }

    }
}

