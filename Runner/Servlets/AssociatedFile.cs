using HSB;
using HSB.Components.Controller;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace Runner;

[Controller("")]
public class StaticFileRoutes
{
    private Response res = null!;

    [Get("/associatedfile1")]
    private void OneMethod()
    {
        res.SendHtmlFile("./static/main.html");
    }

    [Get("/associatedfile2")]
    private void MultipleMethodsGet()
    {
        SendMainHtml();
    }

    [Route("/associatedfile2", HttpMethod.Trace)]
    private void MultipleMethodsTrace()
    {
        SendMainHtml();
    }

    [Get("/associatedfile4")]
    private void MultipleRoutes()
    {
        SendMainHtml();
    }

    private void SendMainHtml()
    {
        res.SendHtmlFile("./static/main.html");
    }
}
