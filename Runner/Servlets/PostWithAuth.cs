using HSB;
using HSB.Components.Controller;
using HSB.Constants;

namespace Runner.TestRunnerClasses;

[Controller("/auth")]
public class PostWithAuth
{
    private Response res = null!;

    [Get("/")]
    private void Get()
    {
        res.Send(HttpCodes.NOT_IMPLEMENTED);
    }

    [Post("/")]
    private void Post()
    {
        res.Send(HttpCodes.NOT_IMPLEMENTED);
    }
}
