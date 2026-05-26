using HSB;
using HSB.Components.Controller;
using HSB.Utils;

namespace Runner;

[Controller("/parameters")]
public class Parameters
{
    private Request req = null!;
    private Response res = null!;

    //example : http://localhost:8080/parameters?param1=1&param2=2&param3=3
    [Get("/")]
    private void Get()
    {
        res.SendHtmlContent($"<h1>Prova GET -> {req.Url}</h1>\nParams : {req.Parameters.DictToString()}");
    }

    [Post("/")]
    private void Post()
    {
        res.Json(req.Parameters);
    }
}
