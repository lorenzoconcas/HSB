using System;
using HSB;
namespace TestRunner
{
    [Binding("/")]
    public class BodyTest_cs : Servlet
    {
        public BodyTest_cs(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            base.ProcessGet(req, res);
            res.Send("<h1>Hello GET</h1>");
        }

        public override void ProcessPost(Request req, Response res)
        {
            base.ProcessPost(req, res);
            res.Send($"<p>{req.rawBody}</p>");
        }


    }
}

