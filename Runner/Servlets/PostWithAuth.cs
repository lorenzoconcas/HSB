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

        public override void ProcessGet()
        {
            //todo
            res.Send(HTTP_CODES.NOT_IMPLEMENTED);
        }

        public override void ProcessPost()
        {
            res.Send(HTTP_CODES.NOT_IMPLEMENTED);
        }
    }
}

