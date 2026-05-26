using HSB;
using HSB.Components.Controller;
using Runner.Models;

namespace Runner;

[Controller("/json")]
public class JsonResponse
{
    private Response res = null!;

    [Get("/")]
    private void Get()
    {
        res.Json(new SimpleObject());
    }

    [Post("/")]
    private void Post()
    {
        res.Json<string>("{'success':true}");
    }
}
