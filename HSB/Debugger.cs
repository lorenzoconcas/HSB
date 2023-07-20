using System.Diagnostics;
using System.Text.Json;

namespace HSB;

public class Debugger
{
    public enum LOG_LEVEL
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
    public LOG_LEVEL logLevel;

    public Debugger()
    {
        enabled = false;
        verbose = true;
        port = 8081;
        address = "127.0.0.1";
        logPath = $"{AppContext.BaseDirectory}/log_{GetDateFormatted()}";
        logLevel = LOG_LEVEL.INFO;

        StartDebugServer();
    }


    public Debugger(bool enabled, bool verbose, int port, string address, string logPath = "",
        LOG_LEVEL logLevel = LOG_LEVEL.INFO)
    {
        this.enabled = enabled;
        this.verbose = verbose;
        this.port = port;
        this.address = address;
        this.logPath = logPath == "" ? $"{AppContext.BaseDirectory}/log_{GetDateFormatted()}" : logPath;
        this.logLevel = logLevel;

        StartDebugServer();
    }

    private void StartDebugServer()
    {
        if (enabled)
        {
            Terminal.DEBUG("Debug server has not been written yet");
        }
    }


    private string GetDateFormatted()
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
            (LOG_LEVEL)json.GetProperty("logLevel").GetInt16()
        );
    }

    public void ERROR<T>(T o, bool printExtraInfo = false)
    {
        if (verbose)
            Terminal.ERROR(o, printExtraInfo);

        AppendToFile(getMessage("E", o.ToString()));
    }

    public void WARNING<T>(T o, bool printExtraInfo = false)
    {
        if (verbose)
            Terminal.WARNING(o, printExtraInfo);

        if (logLevel == LOG_LEVEL.INFO && logLevel == LOG_LEVEL.WARNING && logLevel == LOG_LEVEL.ALL)
            AppendToFile(getMessage("W", o.ToString()));
    }

    public void INFO<T>(T o, bool printExtraInfo = false)
    {
        if (verbose)
            Terminal.INFO(o, printExtraInfo);

        if (logLevel == LOG_LEVEL.INFO && logLevel == LOG_LEVEL.ALL)
            AppendToFile(getMessage("I", o.ToString()));
    }

    public void DEBUG<T>(T o, bool printExtraInfo = false)
    {
        if (verbose)
            Terminal.DEBUG(o, printExtraInfo);

        if (logLevel == LOG_LEVEL.ALL)
            AppendToFile(getMessage("D", o.ToString()));
    }

    private void AppendToFile(string content)
    {
        if (enabled)
        {
            File.AppendAllText(logPath, content);
        }
    }

    private static string getMessage(string lvl, string msg)
    {
        return $"[{DateTime.Now.ToString()}][{lvl}][{msg}]";
    }
}