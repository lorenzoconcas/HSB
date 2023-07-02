using System;
using System.Text.Unicode;
using HSB;
using static System.Net.Mime.MediaTypeNames;

namespace TestRunner.TestRunnerClasses
{
    [Binding("/auth")]
    public class PostWithAuth : Servlet
    {
        public PostWithAuth(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessPost(Request req, Response res)
        {
            if (req.GetHeaders["Content-Type"] == "image/jpeg")
            {

                string body = req.RawBody;


                File.WriteAllBytes("./static/test.jpeg", Convert.FromBase64String(body));
            }

            res.SendCode(200);
        }
    }
}

