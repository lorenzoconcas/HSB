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

    public override void ProcessGet()
    {
        if (req.URL == "/form.html")
        {

            res.SendHTMLContent("<form action=\"/formupload\" method=\"post\">" +
            "<input type=\"text\" name=\"name\" id=\"name\" placeholder=\"Your name\"></input><br>" +
            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
            "</form>");
        }
        else
        {
            res.SendHTMLContent("<h1>404 Not Found</h1>");
        }
    }

    public override void ProcessPost()
    {
        if (req.URL == "/formupload" && req.IsFormUpload())
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
                res.Send(HTTP_CODES.INTERNAL_SERVER_ERROR);
                return;
            }
            res.SendHTMLContent($"<h1>Hello {f.Get("name")}</h1>");

        }
        else
        {
            res.SendCode(HTTP_CODES.FORBIDDEN);
        }

    }
}