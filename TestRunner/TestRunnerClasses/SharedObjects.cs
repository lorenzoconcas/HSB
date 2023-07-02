using System;
using HSB;
using MimeTypes;

namespace TestRunner
{
    [Binding("/sharedbjects")]
    public class SharedObjects : Servlet
    {
        public SharedObjects(Request req, Response res, Configuration c) : base(req, res, c)
        {

        }

        public override void ProcessGet(Request req, Response res)
        {
            res.Send($"<h1>Prova SharedObjects -> {(int)configuration.GetSharedObject("test")}", "text/html");
        }





    }
}



