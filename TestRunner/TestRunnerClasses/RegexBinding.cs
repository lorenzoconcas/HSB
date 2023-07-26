using System;
using HSB;

namespace TestRunner.TestRunnerClasses
{
    [Binding("/regex/", true)]
    public class RegexBinding : Servlet
    {
        public RegexBinding(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet()
        {
            res.Send("<h1>Ciao</h1>", "text/html");
        }
    }
}

