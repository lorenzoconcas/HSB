using System.Reflection;
using HSB.Utils;
using HSB.Utils.HtmlUtils;

namespace HSB.DefaultPages;

public class Index(Response res, Configuration config)
{
    public void Get()
    {
        var page = ResourceUtils.LoadResourceAsString("index.html");
        var image = ResourceUtils.LoadResourceAsString("logo_b64");
        var logo = $"<img width=\"32px\" src=\"{image}\" />";
        res.AddAttribute("logo", logo); 
        res.AddAttribute("footer", PageBuilder.GetFooterString(config));
      

        res.SendHTMLContent(page, true);
    }
}