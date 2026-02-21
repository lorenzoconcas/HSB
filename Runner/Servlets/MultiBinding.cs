using HSB;
namespace Runner
{
    [Binding("/multi1")]
    [Binding("/multi2")]
    public class MultiBinding : Servlet
    {
        public MultiBinding(Request req, Response res) : base(req, res)
        {

        }

        public override void GET()
        { 
            res.SendHTMLContent($"<h1>Hello GET -> {req.Url}</h1>");
        }

        public override void POST()
        {
            res.SendHTMLContent($"<h1>Hello POST -> {req.Url}</h1>");
        }


    }
}

