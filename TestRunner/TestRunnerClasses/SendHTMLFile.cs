using HSB;

namespace TestRunner
{
    [Binding("/htmlfromdisk")]
    public class SendHTMLFile : Servlet
    {
        public SendHTMLFile(Request req, Response res) : base(req, res)
        {
        }

        public override void ProcessGet(Request req, Response res)
        {
            res.SendHTMLPage("./static/index.html");
        }
    }
}
