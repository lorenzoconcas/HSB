using HSB;
using HSB.Components.Controller;
using HSB.Constants;
using HttpMethod = HSB.Constants.HttpMethod;

namespace Runner.Controllers;

[Controller("/controller")]
public class ExampleController
{
    [Get("/")]
    protected void GetRoot(Request req, Response res)
    {
        res.Json(new {
            message = "Hello from the controller!"
        });
    }

    [Get("/:id")]
    private void GetById(Request req, Response res)
    {
        res.Json(new {
            message = $"Hello from the controller with id {req.Parameters["id"]}!"
        });
    }

    [Post("/")]
    private void Post(Request req, Response res)
    {
        res.Json(new
        {
            message = "Hello, this is a POST request!",
        });
    }
    
    [Route("/custom", HttpMethod.Put)]
    private void CustomRoute(Request req, Response res)
    {
        res.Json(new
        {
            message = "Hello, this is a custom route with PUT method!",
        });
    }
}