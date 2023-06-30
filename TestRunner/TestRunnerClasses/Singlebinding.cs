using System;
using HSB;
namespace TestRunner
{
    [Binding("/single1")]
    public class SingleBinding : Servlet
    {
        public SingleBinding(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            base.ProcessGet(req, res);
            res.Send($"<h1>Prova GET -> {req.URL}</h1>\nParams:{req.GetParameters.DictToString()}");
        }

        public override void ProcessPost(Request req, Response res)
        {
            base.ProcessPost(req, res);
            res.Send($"<h1>Prova POST -> {req.URL}</h1>");
        }



    }
}



