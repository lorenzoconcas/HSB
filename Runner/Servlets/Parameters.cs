using HSB;
using HSB.Constants;
namespace Runner
{
    [Binding("/parameters")]
    public class Parameters : Servlet
    {
        public Parameters(Request req, Response res) : base(req, res)
        {

        }
        //example : http://localhost:8080/parameters?param1=1&param2=2&param3=3
        public override void ProcessGet()
        {
            res.SendHTMLContent($"<h1>Prova GET -> {req.URL}</h1>\nParams : {req.Parameters.DictToString()}");
        }

        public override void ProcessPost()
        {
            res.JSON(req.Parameters);

            // res.Send($"<h1>Prova POST -> {req.URL}</h1>", mimeType: MimeType.TEXT_HTML);
        }


    }
}



