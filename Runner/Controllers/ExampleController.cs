using HSB;
using HSB.Components.Controller;
using HSB.Constants;

namespace Runner.Controllers;

[Controller("/controller")]
public class ExampleController
{
    [Get("/")]
    protected void GetRoot(Request req, Response res)
    {
        res.JSON(new {
            message = "Hello from the controller!"
        });
    }

    [Get("/:id")]
    private void GetById(Request req, Response res)
    {
        res.JSON(new {
            message = $"Hello from the controller with id {req.Parameters["id"]}!"
        });
    }

    [Post("/")]
    private void Post(Request req, Response res)
    {
        res.JSON(new
        {
            message = "Hello, this is a POST request!",
        });
    }
    
    [Route("/custom", HTTP_METHOD.PUT)]
    private void CustomRoute(Request req, Response res)
    {
        res.JSON(new
        {
            message = "Hello, this is a custom route with PUT method!",
        });
    }
}