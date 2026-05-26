using HSB;
using HSB.Components.Controller;

namespace Runner;

[Controller("/htmlfromdisk")]
public class SendHTMLFile
{
    private Response res = null!;

    [Get("/")]
    private void Get()
    {
        res.SendHtmlFile("./static/main.html");
    }
}
