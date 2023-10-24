using HSB;
using HSB.Constants;
Configuration c = new();

c.GET("/", (Request req, Response res) =>
{
    //send a complex form
    var html = "<html><head></head></body>" +
                "<form action='/form' method='POST'>" +
               "<input type='text' name='name' placeholder='name' />" +
               "<input type='text' name='surname' placeholder='surname' />" +
               "<input type='submit' value='send' />" +
               "</form>" +
               "</body></html>";
    res.SendHTMLContent(html);
});

c.POST("/form", (Request req, Response res) =>
{
    if (!req.IsFormUpload())
    {
        res.Send(HttpCodes.BAD_REQUEST);
        return;
    }
    var form = req.GetFormData();
    if (form == null)
    {
        res.Redirect("/");
        return;
    }
    //read form data
    var name = form.Get("name");
    var surname = form.Get("surname");
    res.SendHTMLContent($"<h1>Hello {name} {surname}</h1>");
});


new Server(c).Start();