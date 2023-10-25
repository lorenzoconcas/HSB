using System;
using HSB;

namespace Manager.@static
{
    [Binding("/vendor", true)]
    public class Vendor : Servlet
    {
        public Vendor(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet()
        {
            if (req.Parameters.ContainsKey("file"))
            {
                string file = req.Parameters["file"];
                string content = HSB.Utils.LoadResourceString(file);
                Terminal.INFO($"Serving '{file}' from resources (if found)");
                res.Send(content);
            }
            else
            {
                res.Send(404);
            }
        }
    }
}

