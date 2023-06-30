using System;
using HSB;
namespace TestRunner
{
    [Binding("/json")]
    public class JsonResponse : Servlet
    {
        public JsonResponse(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            base.ProcessGet(req, res);
            res.JSON("{'success':true}");
        }

        public override void ProcessPost(Request req, Response res)
        {
            res.JSON("{'success':true}");
        }



    }
}



