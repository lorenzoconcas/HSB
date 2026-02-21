using System.Reflection;

namespace HSB.Utils;

public static class CliUtils
{
    public static void PrintLogo()
    {
        Terminal.Write("Welcome to ");
        Terminal.Write("H", BgColor.DEFAULT, FgColor.RED);
        Terminal.Write("S", BgColor.DEFAULT, FgColor.GREEN);
        Terminal.Write("B", BgColor.DEFAULT, FgColor.BLUE);
        Terminal.Write("-", BgColor.DEFAULT, FgColor.WHITE);
        Terminal.Write("#", BgColor.DEFAULT, FgColor.YELLOW);
        Terminal.Write(" (Http Server Boxed)");
        Terminal.Write($" v{Assembly.GetExecutingAssembly().GetName().Version}");
        Terminal.WriteLine($" - PID {Environment.ProcessId}");
    }
}