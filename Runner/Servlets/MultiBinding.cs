using HSB;
using HSB.Components.Controller;

namespace Runner;

[Controller("")]
public class MultiBinding
{
    private Request req = null!;
    private Response res = null!;

    [Get("/multi1")]
    private void GetMulti1()
    {
        SendGet();
    }

    [Get("/multi2")]
    private void GetMulti2()
    {
        SendGet();
    }

    [Post("/multi1")]
    private void PostMulti1()
    {
        SendPost();
    }

    [Post("/multi2")]
    private void PostMulti2()
    {
        SendPost();
    }

    private void SendGet()
    {
        res.SendHtmlContent($"<h1>Hello GET -> {req.Url}</h1>");
    }

    private void SendPost()
    {
        res.SendHtmlContent($"<h1>Hello POST -> {req.Url}</h1>");
    }
}
