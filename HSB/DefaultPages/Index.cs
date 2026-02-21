using System.Reflection;
using HSB.Utils;

namespace HSB.DefaultPages;

public class Index(Response res, Configuration config)
{
    public void Get()
    {
        var page = ResourceUtils.LoadResourceAsString("index.html");
        var version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }


        string serverName;
        var footerDiv = "";
        var logo = "";
        var currentYear = DateTime.Now.Year;
        //string title = "";
        if (config.CustomServerName != "")
        {
            serverName = config.CustomServerName;
        }
        else
        {
            var logoB64 = ResourceUtils.LoadResourceAsString("logo_b64");
            serverName = "HSB<sup>#</sup>";
            footerDiv = $"<div class=\"footer\">Copyright &copy; 2021-{currentYear} Lorenzo L. Concas</div>";
            logo = $"<img width=\"32px\" src=\"{logoB64}\" />";
            // title = "Http Server Boxed <sup>#</sup>";
        }

        //set attributes
        res.AddAttribute("logo", logo); //this break some configurations, logo must be replaced with a smaller image
        //  res.AddAttribute("title", title);
        res.AddAttribute("serverName", serverName);
        res.AddAttribute("footer_div", footerDiv);
        res.AddAttribute("hsbVersion", version);

        res.SendHTMLContent(page, true);
    }
}