using HSB;
using HSB.Components;
using HSB.Constants;
namespace Runner;


[Binding("/form.html")]
[Binding("/formupload")]
public class Form : Servlet
{
    private const string savePath = "./uploaded";
    public Form(Request req, Response res) : base(req, res)
    {

    }

    public override void GET()
    {
        if (req.Url == "/form.html")
        {

            res.SendHtmlContent("<form action=\"/formupload\" method=\"post\">" +
            "<input type=\"text\" name=\"name\" id=\"name\" placeholder=\"Your name\"></input><br>" +
            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
            "</form>");
        }
        else
        {
            res.SendHtmlContent("<h1>404 Not Found</h1>");
        }
    }

    public override void POST()
    {
        if (req.Url == "/formupload" && req.IsFormUpload())
        {
            //note that only the first element with a given name is stored inside the FormData
            //  for example a form like this
            //  <form ...>
            //      <input type="text" name="name" id="name" placeholder="Your name"></input>
            //      <input type="text" name="name" id="surname" placeholder="Your surname"></input>
            //  </form>
            // will result in Form containing only
            // {name: "value of name"}
            var f = req.GetFormData();
            if (f == null)
            {
                res.Send(HttpCodes.INTERNAL_SERVER_ERROR);
                return;
            }
            res.SendHtmlContent($"<h1>Hello {f.Get("name")}</h1>");

        }
        else
        {
            res.SendCode(HttpCodes.FORBIDDEN);
        }

    }
}