using HSB;
using HSB.Components.Controller;

namespace Documentation;

[Controller("/api")]
public class ApiExample
{
    private Response res = null!;

    [Get("/")]
    private void Get()
    {
        res.SendJson("{}");
    }

    [Get("/example")]
    private void GetExample()
    {
        res.SendJson("{}");
    }
}
