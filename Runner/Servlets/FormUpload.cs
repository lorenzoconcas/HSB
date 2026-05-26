using HSB;
using HSB.Components.Controller;
using HSB.Constants;

namespace Runner;

[Controller("")]
public class Form
{
    private Request req = null!;
    private Response res = null!;

    [Get("/form.html")]
    private void GetForm()
    {
        res.SendHtmlContent("<form action=\"/formupload\" method=\"post\">" +
                            "<input type=\"text\" name=\"name\" id=\"name\" placeholder=\"Your name\"></input><br>" +
                            "<input type=\"submit\" value=\"Upload\" name=\"submit\">" +
                            "</form>");
    }

    [Post("/formupload")]
    private void Upload()
    {
        if (!req.IsFormUpload())
        {
            res.SendCode(HttpCodes.FORBIDDEN);
            return;
        }

        var form = req.GetFormData();
        if (form == null)
        {
            res.Send(HttpCodes.INTERNAL_SERVER_ERROR);
            return;
        }

        res.SendHtmlContent($"<h1>Hello {form.Get("name")}</h1>");
    }
}
