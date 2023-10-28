using System.Reflection;
using HSB;
internal class ManagerMain
{
    private static void Main(string[] args)
    {
        Configuration c = new()
        {
            Port = 65001,
            Address = "127.0.0.1",
            StaticFolderPath = "./static"
        };

        c.AddCustomGlobalHeader("Set-Cookie", $"managerVersion={Assembly.GetExecutingAssembly().GetName().Version}");


        new Server(c).Start();
    }
}