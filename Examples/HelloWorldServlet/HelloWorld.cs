using HSB;

namespace Example
{
    [Binding("/")] //route the root page
    public class HelloWorld : Servlet
    {
        public HelloWorld(Request req, Response res) : base(req, res)
        {
        }

        //we override the function that handle the GET response processing
        public override void ProcessGet()
        {
            //reply to request with an hello world
            res.SendHTMLContent("<h1>Hello world</h1>");
        }


        //same for the post request
        public override void ProcessPost()
        {
            //if no change are made, the server will reply with code 405
        }
    }
}