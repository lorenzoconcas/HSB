using System.Reflection;
using HSB;
internal class ManagerMain
{
    private static void Main(string[] args)
    {
        //the manager runs at port 65001 generally
        Configuration c = new()
        {
            port = 65001,
            address = "127.0.0.1",
            staticFolderPath = "./static"
        };

        c.AddCustomGlobalHeader("Set-Cookie", $"managerVersion={Assembly.GetExecutingAssembly().GetName().Version}");


        //c.AddCustomGlobalHeader("Set-Cookie", $"hasbVersion={Assembly.GetExecutingAssembly().GetName().Version}");

        new Server(c).Start();
    }
}