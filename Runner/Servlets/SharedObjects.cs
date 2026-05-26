using HSB;
using HSB.Components.Controller;

namespace Runner;

[Controller("/sharedobjects")]
public class SharedObjects
{
    private Response res = null!;

    [Get("/")]
    private void Get(Configuration configuration)
    {
        int item = (int)configuration.GetSharedObject("test");
        res.SendHtmlContent($"<h1>Prova SharedObjects -> {item}</h1>");
    }
}
