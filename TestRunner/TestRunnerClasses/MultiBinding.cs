using System;
using HSB;
namespace TestRunner
{
    [Binding("/")]
    [Binding("/info")]
    public class MultiBinding : Servlet
    {
        public MultiBinding(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            base.ProcessGet(req, res);
            res.Send($"<h1>Hello GET -> {req.URL}</h1>");
        }

        public override void ProcessPost(Request req, Response res)
        {
            base.ProcessPost(req, res);
            res.Send($"<h1>Hello POST -> {req.URL}</h1>");
        }


    }
}

