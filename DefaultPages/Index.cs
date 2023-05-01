using System;
using System.Reflection;

namespace HSB
{
    public class Index : Servlet
    {
        public Index(Request req, Response res) : base(req, res)
        {

        }
        public override void ProcessGet(Request req, Response res)
        {
            res.Send($"<h1>Welcome to HSB-# ({Assembly.GetExecutingAssembly().GetName().Version})</h1>", MimeType.TEXT_HTML);
        }
    }
}

