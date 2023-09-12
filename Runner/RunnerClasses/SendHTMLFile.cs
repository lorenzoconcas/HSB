using HSB;

namespace Runner
{
    [Binding("/htmlfromdisk")]
    public class SendHTMLFile : Servlet
    {
        public SendHTMLFile(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet()
        {
            res.SendHTMLPage("./static/index.html");
        }
    }
}
