using System;
using HSB;
using MimeTypes;

namespace TestRunner
{
    [Binding("/customobject")]
    public class CustomObjects : Servlet
    {
        public CustomObjects(Request req, Response res, Configuration c) : base(req, res, c)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            base.ProcessGet(req, res);
            res.Send($"<h1>Prova CustomObjects -> {(int)configuration.GetSharedObject("test")}", "text/html");
        }





    }
}



