namespace StressTest;
using HSB;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Constants;
using HSB.OpenApi.Attributes;
[Controller("/TestController")]
[ApiTag("Example Controller")]
public class TestController
{
    public Request req;
    private Response res;
    [Get("/")]
    [ApiSummary("Get root endpoint")]
    [ApiDescription("Used when called the root of this controller, returns a simple message.")]
    protected void GetRoot()
    {
        res.Json(new
        {
            message = "Hello from the controller!"
        });
    }
}