using System.Text.Json;

namespace HSB;

public class Debugger
{
    public enum LogLevel
    {
        ERROR,
        WARNING,
        INFO,
        ALL
    }

    public bool enabled;
    public bool verbose;
    public int port;
    public string address;
    public string logPath;
    public LogLevel logLevel;

    //Todo : add an authentication method to access the socket of the debugger
    //Todo : establish communication rules between debugger and client

    public Debugger()
    {
        enabled = false;
        verbose = true;
        port = 8081;
        address = "127.0.0.1";
        logPath = Path.Combine(AppContext.BaseDirectory, $"log_{GetDateFormatted()}");
        logLevel = LogLevel.INFO;

        StartDebugServer();
    }


    public Debugger(bool enabled, bool verbose, int port, string address, string logPath = "",
        LogLevel logLevel = LogLevel.INFO)
    {
        this.enabled = enabled;
        this.verbose = verbose;
        this.port = port;
        this.address = address;
        this.logPath = logPath == "" ? $"{AppContext.BaseDirectory}log_{GetDateFormatted()}" : logPath;
        this.logLevel = logLevel;

        StartDebugServer();
    }

    private void StartDebugServer()
    {
        if (enabled)
        {
            Terminal.DEBUG("Debug server has not been written yet");
            //The writing of this feature is complex as is the rest of the entire project
        }
    }


    private static string GetDateFormatted()
    {
        return new DateTime().ToString("yyyy_mm_dd_hh_MM");
    }

    public static Debugger FromJson(JsonElement json)
    {
        return new Debugger(
            json.GetProperty("enabled").GetBoolean(),
            json.GetProperty("verbose").GetBoolean(),
            json.GetProperty("port").GetInt16(),
            json.GetProperty("address").GetString() ?? "127.0.0.1",
            json.GetProperty("logPath").GetString() ?? "",
            (LogLevel)json.GetProperty("logLevel").GetInt16()
        );
    }

    public void ERROR<T>(T o, bool printExtraInfo = true)
    {
        if (verbose)
            Terminal.ERROR(o, printExtraInfo);
        if (o != null)
        {
            var msg = o.ToString() ?? "";
            AppendToFile(GetMessage("E", msg));
        }
    }

    public void WARNING<T>(T o, bool printExtraInfo = true)
    {
        if (verbose)
            Terminal.WARNING(o, printExtraInfo);

        if (logLevel == LogLevel.INFO && logLevel == LogLevel.WARNING && logLevel == LogLevel.ALL && o != null)
        {
            var msg = o.ToString() ?? "";
            AppendToFile(GetMessage("W", msg));
        }
    }

    public void INFO<T>(T o, bool printExtraInfo = true)
    {
        if (verbose)
            Terminal.INFO(o, printExtraInfo);

        if (logLevel == LogLevel.INFO && logLevel == LogLevel.ALL && o != null)
        {
            var msg = o.ToString() ?? "";
            AppendToFile(GetMessage("I", msg));
        }
    }

    public void DEBUG<T>(T o, bool printExtraInfo = true)
    {
        if (verbose)
            Terminal.DEBUG(o, printExtraInfo);
        if (logLevel == LogLevel.ALL && o != null)
        {
            var msg = o.ToString() ?? "";
            AppendToFile(GetMessage("D", msg));
        }
    }

    private void AppendToFile(string content)
    {
        if (enabled)
        {
            File.AppendAllText(logPath, content);
        }
    }

    private static string GetMessage(string lvl, string msg)
    {
        return $"[{GetDateFormatted()}][{lvl}][{msg}]";
    }
}