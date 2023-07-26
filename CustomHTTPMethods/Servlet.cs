﻿using HSB;
namespace CustomHTTPMethods
{
    [Binding("/")]
    class Servlet: HSB.Servlet
    {
        public Servlet(Request req, Response res): base(req, res)
        {
            AddCustomMethodHandler("MyCustomHTTPMethod", ProcessMyCustomHTTPMethod);
        }

        private void ProcessMyCustomHTTPMethod(Request req, Response res)
        {
            res.SendHTMLContent("<h1>Hello</h1>");
        }
    }
}
