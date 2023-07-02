using System;
using HSB;
using TestRunner.TestRunnerModels;

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
            res.JSON(new SimpleObject());
        }

        public override void ProcessPost(Request req, Response res)
        {
            res.JSON("{'success':true}");
        }



    }
}



