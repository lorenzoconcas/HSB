/// <summary>
/// This project is written to show (and test) how to write platform-independent
/// HSB-based applications, launchable via the "Launcher" project
/// </summary>


public class Program
{

    public static void HSBMain(string[] args)
    {
        HSB.Configuration c = new()
        {
            Port = 8080,
            Address = "127.0.0.1",
            StaticFolderPath = "./static"
        };

        new HSB.Server(c).Start();

    }
}