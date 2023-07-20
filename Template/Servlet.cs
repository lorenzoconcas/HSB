using System;
using HSB;

namespace Template
{
    [Binding("/")]
    public class Servlet : HSB.Servlet
    {
        public Servlet(Request req, Response res) : base(req, res)
        {
        }
    }
}

