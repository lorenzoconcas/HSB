using System;
using HSB;

namespace Runner.Servlets
{
    [Binding(@"/`\/.*\/*regex.")]
    public class RegexBinding : Servlet
    {
        public RegexBinding(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet()
        {
         
            res.Send($@"<h1>Hi</h1><h4> you used regex /`\/.*\/*regex. to match {req.URL}</h4>", "text/html");
        }
    }
}

