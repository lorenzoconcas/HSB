using System.Text.Json.Serialization;
using HSB;
using HSB.Components.Controller;
using HSB.Constants;
using HSB.OpenApi.Attributes;
using HttpMethod = HSB.Constants.HttpMethod;

namespace ControllerExample;

[ApiResponseModel()]
interface IResponse
{
    [JsonPropertyName("message")]
    string Message { get; set; }
}

[ApiResponseModel()]
interface IComplexResponse
{
    [JsonPropertyName("message")]
    string Message { get; set; }
    
    [JsonPropertyName("data")]
    object Data { get; set; }
}


[Controller("/controller")]
[ApiTag("Example Controller")]
public class ExampleController
{
    [Get("/")]
    [ApiSummary("Get root endpoint")]
    [ApiDescription("Used when called the root of this controller, returns a simple message.")]
    protected void GetRoot(Request req, Response res)
    {
        res.JSON(new
        {
            message = "Hello from the controller!"
        });
    }

    [Get("/:id")]
    [ApiSummary("An example of a route with a parameter")]
    [ApiDescription("Used when called with an id parameter, returns a simple message with the id.")]
    [ApiParameter("id", "The id parameter from the route", true, "string")]
    [ApiResponse(HttpCodes.OK, "Successful response", "IResponse") ]
    private void GetById(Request req, Response res)
    {
        if (!req.Parameters.ContainsKey("id"))
        {
            res.JSON(new
            {
                message = "Hello from the controller!, did you forgot the id parameter?"
            }, statusCode: HttpCodes.BAD_REQUEST);
            return;
        }

        res.JSON(new
        {
            message = $"Hello from the controller with id {req.Parameters["id"]}!"
        });
    }

    [Post("/")]
    [ApiSummary("An example of a POST route")]
    [ApiDescription("Used when called with a POST request, returns a simple message.")]
    [ApiResponse(HttpCodes.OK, "Successful response")]
    private void Post(Request req, Response res)
    {
        res.JSON(new
        {
            message = "Hello, this is a POST request!",
        });
    }

    [Route("/custom", HttpMethod.Put)]
    [ApiSummary("An example of a custom route with PUT method")]
    [ApiDescription("Used when called with a PUT request to /custom, returns a simple message.")]
    [ApiResponse(HttpCodes.OK, "Successful response")]
    private void CustomRoute(Request req, Response res)
    {
        res.JSON(new
        {
            message = "Hello, this is a custom route with PUT method!",
        });
    }
}