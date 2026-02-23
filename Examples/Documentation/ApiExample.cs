using HSB;
using HSB.Components.Attributes;

namespace Documentation;


[Binding("/api")]
[Binding("/api/example")]
public class ApiExample(Request req, Response res) : Servlet(req, res)
{

    override public void GET()
    {
        res.SendJSON("{}");
    }
}

[Binding()]
public class Autobind(Request req, Response res) : Servlet(req, res)
{

}