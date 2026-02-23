using System.Reflection;
using System.Xml;

namespace HSB.Utils.HtmlUtils;

public class PageBuilder(string title)
{
    private const string BaseStyleResourceName = "common.css";

    private readonly string style = ResourceUtils.LoadResource<string>(BaseStyleResourceName, "HSB.Resources") ?? "";
    private string body = "";

    private string Title = title;


    //starts loading resources


    private static string InsertValue(string html, string key, string value)
    {
        return html.Replace($"#{{{key}}}", value);
    }

    private static string InsertValues(string html, Dictionary<string, string> keyValues)
    {
        return keyValues.Aggregate(html, (current, keyValue) => current.Replace(keyValue.Key, keyValue.Value));
    }


    public void AddCard(string title, string content, string footer)
    {
        body += $"""
                 <div class="card">
                     <div class="header">
                         <div class="title">{title}</div>

                     </div>

                     <div class="content">
                         <div id="routePath" class="route-box">
                             {content}
                         </div>
                     </div>

                     <div class="footer">
                         {footer}
                     </div>
                 </div>
                 """;
    }

    public static string GetScrollableList<T>(List<T> items, Delegate? dateModifier = null)
    {
        var content = "";
        if (dateModifier != null)
        {
            content = dateModifier?.DynamicInvoke(items) as string;
        }
        else
        {
            items.ForEach(t => content += $"<span>{t?.ToString()}</span>");
        }

        return $"""
                <div class="scrollable-table">
                    {content}
                </div>
                """;
    }

    public static string GetFooterString(Configuration c)
    {
        return c.CustomServerName ??
               $"HSB v{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"} &bull; Copyright &copy 2021-{DateTime.Now.Year} HSB Team";
    }

    public string Render()
    {
        return $"""
                <!DOCTYPE html>
                <html lang="en">

                <head>
                    <title>#{Title}</title>
                    <style>
                        {style}
                    </style>
                </head>

                <body>
                {body}
                </body>

                </html>
                """;
    }
}