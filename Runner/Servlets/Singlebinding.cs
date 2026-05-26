using HSB;
using HSB.Components.Controller;
using HSB.Utils;

namespace Runner;

[Controller("/single1")]
public class SingleBinding
{
    private Request req = null!;
    private Response res = null!;

    [Get("/")]
    private void Get()
    {
        res.SendHtmlContent($"<h1>Prova GET -> {req.Url}</h1>\nParams:{req.Parameters.DictToString()}");
    }

    [Post("/")]
    private void Post()
    {
        res.SendHtmlContent($"<h1>Prova POST -> {req.Url}</h1>");
    }
}
