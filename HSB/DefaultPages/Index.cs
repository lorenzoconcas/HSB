using System;
using System.Reflection;
using HSB.Constants;

namespace HSB;

public class Index(Request req, Response res, Configuration config) : Servlet(req, res, config)
{
    public override void ProcessGet()
    {
        string page = ReadFromResources("index.html");
        string version = "v";
        if (Assembly.GetExecutingAssembly().GetName().Version != null)
        {
            version += Assembly.GetExecutingAssembly().GetName().Version!.ToString();
        }



        string footer_div = "";
        string server_name;
        string logo = "";
        string title = "";
        if (configuration.CustomServerName != "")
        {
            server_name = configuration.CustomServerName;
        }
        else
        {
            server_name = "HSB<sup>#</sup>";
            footer_div = "<div class=\"footer\">Copyright &copy; 2021-2023 Lorenzo L. Concas</div>";
            string logo_b64 = ReadFromResources("logo_b64");
            logo = $"<img width=\"32px\" src=\"{logo_b64}\" />";
            title = "Http Server Boxed <sup>#</sup>";
        }

        //set attributes
        res.AddAttribute("logo", logo); //this break some configurations, logo must be replaced with a smaller image
        res.AddAttribute("title", title);
        res.AddAttribute("serverName", server_name);
        res.AddAttribute("footer_div", footer_div);
        res.AddAttribute("hsbVersion", version);

        res.SendHTMLContent(page, true);

    }
}

