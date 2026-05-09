using System.Reflection;
using HSB.Utils;
using HSB.Utils.HtmlUtils;

namespace HSB;

public class Error(Response res, Configuration config, string errorMessage, int errorCode)
{
    private string errId = "genericError";


    public void Throw()
    {

        var builder = new PageBuilder(errorCode.ToString());
        
        builder.AddCard(errorCode.ToString(), errorMessage,  PageBuilder.GetFooterString(config));
        
        res.SendHtmlContent(builder.Render(), statusCode: errorCode);
        return;
        
        
/*
#if DEBUG
        var debugMode = true;
#else
        var debugMode = false;
#endif

        var title = $"{errorCode}";
        var content = "";

        switch (errorCode)
        {
            case >= 500 and <= 599:
            {
                if (debugMode || config.Debug.enabled)
                    content = GetStacktracePage();
                else content = Get5XxPage();

                errId = "stacktrace";
                break;
            }
            case >= 400 and <= 499:
                content = Get4XxPage(errorMessage);
                break;
        }


        Send(title, content, errorCode);*/
    }

    private string GetStacktracePage() => "Stacktrace:<br>" + errorMessage.Replace("\n", "<br>");

    private static string Get4XxPage(string errorMessage = "The requested resource was not found on this server") =>
        $"<h3>{errorMessage.Replace("\n", "<br>")}</h3>";

    private static string Get5XxPage() => $"<h3>An internal error occurred while elaborating the request</h3>";

    private void Send(string title, string msg, int statusCode)
    {
        
        
        
        var page = ResourceUtils.LoadResourceAsString("error.html");

        var version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }

        var footerDiv = "";
        var serverName = PageBuilder.GetFooterString(config);
        res.AddAttribute("page_title", "Error " + errorCode);
        res.AddAttribute("serverName", serverName);
        res.AddAttribute("footer_div", footerDiv);
        res.AddAttribute("hsbVersion", version);
        res.AddAttribute("title", title);
        res.AddAttribute("errorMsg", msg);
        res.AddAttribute("errID", errId);
        res.SendHtmlContent(page, true, statusCode: statusCode);
    }
}