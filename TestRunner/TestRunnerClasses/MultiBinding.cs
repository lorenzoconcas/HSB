using HSB;
namespace TestRunner
{
    [Binding("/multi1")]
    [Binding("/multi2")]
    public class MultiBinding : Servlet
    {
        public MultiBinding(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            res.SendHTMLContent($"<h1>Hello GET -> {req.URL}</h1>");
        }

        public override void ProcessPost(Request req, Response res)
        {
            res.SendHTMLContent($"<h1>Hello POST -> {req.URL}</h1>");
        }


    }
}

