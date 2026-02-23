using System.Reflection;

namespace HSB.Utils;

public static class CliUtils
{
    public static void PrintLogo()
    {
        Terminal.Write("Welcome to ");
        Terminal.Write("H", BgColor.Default, FgColor.Red);
        Terminal.Write("S", BgColor.Default, FgColor.Green);
        Terminal.Write("B", BgColor.Default, FgColor.Blue);
        Terminal.Write("-", BgColor.Default, FgColor.White);
        Terminal.Write("#", BgColor.Default, FgColor.Yellow);
        Terminal.Write(" (Http Server Boxed)");
        Terminal.Write($" v{Assembly.GetExecutingAssembly().GetName().Version}");
        Terminal.WriteLine($" - PID {Environment.ProcessId}");
    }
}