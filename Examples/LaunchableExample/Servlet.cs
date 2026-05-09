using HSB;

namespace Test
{
    [Binding("/launcher")]
    public class Servlet : HSB.Servlet
    {
        public Servlet(Request req, Response res) : base(req, res)
        {
        }

        public override void GET()
        {
            res.SendHtmlContent("<h1>Hello there</h1>");
        }

    }
}


