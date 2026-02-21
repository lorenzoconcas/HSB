using System;
using HSB;
using HSB.Utils;
namespace Runner
{
    [Binding("/single1")]
    public class SingleBinding : Servlet
    {
        public SingleBinding(Request req, Response res) : base(req, res)
        {

        }

        public override void GET()
        {
            res.SendHTMLContent($"<h1>Prova GET -> {req.Url}</h1>\nParams:{req.Parameters.DictToString()}");
        }

        public override void POST()
        {
            res.SendHTMLContent($"<h1>Prova POST -> {req.Url}</h1>");
        }



    }
}



