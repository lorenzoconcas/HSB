using System.Text.Json.Serialization;
using HSB;
using HSB.Components.Attributes;
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
    //Request and response are automatically injected into the controller methods
    
    public Request req;
    private Response res;
    
    
    [Get("/")]
    [ApiSummary("Get root endpoint")]
    [ApiDescription("Used when called the root of this controller, returns a simple message.")]
    protected void GetRoot()
    {
        res.JSON(new
        {
            message = "Hello from the controller!"
        });
    }

    [Get("/sub/:id")]
    [ApiSummary("An example of a route with a parameter")]
    [ApiDescription("Used when called with an id parameter, returns a simple message with the id.")]
    [ApiParameter("id", "The id parameter from the route", true, "int")]
    [ApiResponse(HttpCodes.OK, "Successful response", "IResponse") ]
    private void GetById([NamedParameter("id", true)] int id)
    {
        res.JSON(new
        {
            message = $"Hello from the controller with id {id}!"
        });
    }
    
    [Get("/sub/:id/ter/")]
    [ApiSummary("An example of a route with a parameter")]
    [ApiDescription("Used when called with an id parameter, returns a simple message with the id.")]
    [ApiParameter("id", "The id parameter from the route", true, "int")]
    [ApiResponse(HttpCodes.OK, "Successful response", "IResponse") ]
    private void GetByIdBis([NamedParameter("id", true)] int id)
    {
        res.JSON(new
        {
            message = $"Hello from the controller with id {id} (ter)!"
        });
    }
    
    [Get("/sub/:id/ter/:name")]
    [ApiSummary("An example of a route with a parameter")]
    [ApiDescription("Used when called with an id parameter, returns a simple message with the id.")]
    [ApiParameter("id", "The id parameter from the route", true, "int")]
    [ApiParameter("name", "The name parameter from the route", true, "string")]
    [ApiResponse(HttpCodes.OK, "Successful response", "IResponse") ]
    private void GetByIdAndName([NamedParameter("id", true)] int id, [NamedParameter("name", true)] string name)
    {
        res.JSON(new
        {
            message = $"Hello from the controller with id {id} with name {name}!"
        });
    }
    
    [Get("/param")]
    [ApiSummary("An example of a route with a parameter")]
    [ApiDescription("Used when called with an id parameter, returns a simple message with the id.")]
    [ApiParameter("id", "The id parameter from the route", true, "int")]
    [ApiResponse(HttpCodes.OK, "Successful response", "IResponse") ]
    private void GetByParamId([NamedParameter("id", true)] int id)
    {
        res.JSON(new
        {
            message = $"Hello from the controller with id {id}!"
        });
    }

    [Post("/")]
    [ApiSummary("An example of a POST route")]
    [ApiDescription("Used when called with a POST request, returns a simple message.")]
    [ApiResponse(HttpCodes.OK, "Successful response")]
    private void Post()
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
    private void CustomRoute()
    {
        res.JSON(new
        {
            message = "Hello, this is a custom route with PUT method!",
        });
    }

    [Route("/test", HttpMethod.Get)]
    private void TestClassReqAndRes()
    {
        Terminal.INFO(res);
        res?.JSON(new
        {
            message = "Hello, this is a custom route with PUT method!",
        });
    }
}