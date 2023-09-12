using System;
using HSB;
using Runner.RunnerModels;

namespace Runner
{
    [Binding("/json")]
    public class JsonResponse : Servlet
    {
        public JsonResponse(Request req, Response res) : base(req, res)
        {

        }

        public override void ProcessGet()
        {
            res.JSON(new SimpleObject());
        }

        public override void ProcessPost()
        {
            res.JSON("{'success':true}");
        }



    }
}



