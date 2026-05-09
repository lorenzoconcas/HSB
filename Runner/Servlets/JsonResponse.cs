using System;
using HSB;
using Runner.Models;

namespace Runner
{
    [Binding("/json")]
    public class JsonResponse : Servlet
    {
        public JsonResponse(Request req, Response res) : base(req, res)
        {

        }

        public override void GET()
        {
            res.Json(new SimpleObject());
        }

        public override void POST()
        {
            res.Json<string>("{'success':true}");
        }



    }
}



