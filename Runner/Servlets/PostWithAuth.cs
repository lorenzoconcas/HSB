using System;
using System.Text.Unicode;
using HSB;
using HSB.Constants;
using static System.Net.Mime.MediaTypeNames;

namespace Runner.TestRunnerClasses
{
    [Binding("/auth")]

    public class PostWithAuth : Servlet
    {
        public PostWithAuth(Request req, Response res) : base(req, res)
        {
        }

        public override void GET()
        {
            //todo
            res.Send(HttpCodes.NOT_IMPLEMENTED);
        }

        public override void POST()
        {
            res.Send(HttpCodes.NOT_IMPLEMENTED);
        }
    }
}

