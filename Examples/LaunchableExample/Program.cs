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
            port = 8080,
            address = "127.0.0.1",
            staticFolderPath = "./static"
        };

        new HSB.Server(c).Start();

    }
}