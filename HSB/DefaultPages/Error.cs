using System.Reflection;
using HSB.Utils;

namespace HSB;

public class Error(Response res, Configuration config, string errorMessage, int errorCode)
{
    private string errId = "genericError";


    public void Throw()
    {
#if DEBUG
        var debugMode = true;
#else
        var debugMode = false;
#endif

        var title = $"Error {errorCode}";
        var content = "";

        switch (errorCode)
        {
            case >= 500 and <= 599:
            {
                if (debugMode || config.Debug.enabled)
                    content = GetStacktracePage();
                else content = Get5XXPage();

                errId = "stacktrace";
                break;
            }
            case >= 400 and <= 499:
                content = Get4XXPage();
                break;
        }


        Send(title, content, errorCode);
    }

    private string GetStacktracePage() => "Stacktrace:<br>" + errorMessage.Replace("\n", "<br>");
    private static string Get4XXPage() => $"<h3>The requested resource was not found on this server</h3>";
    private static string Get5XXPage() => $"<h3>An internal error occurred while elaborating the request</h3>";

    private void Send(string title, string msg, int statusCode)
    {
        var page = ResourceUtils.LoadResourceAsString("error.html");

        var version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }

        var footerDiv = "";
        var serverName = "";
        if (config.CustomServerName != "")
        {
            serverName = config.CustomServerName;
        }
        else
        {
            serverName = "HSB<sup>#</sup>";
            var currentYear = DateTime.Now.Year;
            footerDiv = $"<div class=\"footer\">Copyright &copy; 2021-{currentYear} Lorenzo L. Concas</div>";
        }

        res.AddAttribute("page_title", "Error " + errorCode);
        res.AddAttribute("serverName", serverName);
        res.AddAttribute("footer_div", footerDiv);
        res.AddAttribute("hsbVersion", version);
        res.AddAttribute("title", title);
        res.AddAttribute("errorMsg", msg);
        res.AddAttribute("errID", errId);
        res.SendHTMLContent(page, true, statusCode: statusCode);
    }
}