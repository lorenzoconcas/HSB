using HSB;
namespace TestRunner
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
            res.Send($"<h1>Prova GET -> {req.URL}</h1>\nParams : {req.GetParameters.DictToString()}",
                mimeType: MimeType.TEXT_HTML);
        }

        public override void ProcessPost()
        {
            res.JSON(req.GetParameters);

            // res.Send($"<h1>Prova POST -> {req.URL}</h1>", mimeType: MimeType.TEXT_HTML);
        }


    }
}



