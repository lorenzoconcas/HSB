using HSB;

namespace Test
{
    [Binding("/launcher")]
    public class Servlet : HSB.Servlet
    {
        public Servlet(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet(Request req, Response res)
        {
            res.SendHTMLContent("<h1>Hello there</h1>");
        }

    }
}


