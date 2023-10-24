using HSB;

namespace Runner
{
    [Binding("/sharedobjects")]
    public class SharedObjects : Servlet
    {
        public SharedObjects(Request req, Response res, Configuration c) : base(req, res, c)
        {

        }

        public override void ProcessGet()
        {
            int item = (int)configuration.GetSharedObject("test");
            res.SendHTMLContent($"<h1>Prova SharedObjects -> {item}</h1>");
        }

    }
}
