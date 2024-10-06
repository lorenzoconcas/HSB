using HSB;
using HSB.Components.Attributes;

namespace Documentation;

[DocumentClass("This is an example of an API endpoint.")]
[Binding("/api")]
[Binding("/api/example")]
public class ApiExample(Request req, Response res) : Servlet(req, res)
{

    override public void ProcessGet()
    {
        res.SendJSON("{}");
    }
}

[Binding()]
public class Autobind(Request req, Response res) : Servlet(req, res)
{

}