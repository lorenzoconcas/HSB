using System;
using HSB;
namespace TestRunner
{
    public class HSBTestRunner
    {
        private static void Main(string[] args)
        {
            Terminal.INFO("HELLO");
            Configuration c = new Configuration()
            {
                address = "127.0.0.1",
                port = 8080
            };
            _ = new Server(c);
        }

    }
}

