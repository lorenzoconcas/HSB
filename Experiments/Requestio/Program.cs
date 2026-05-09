using HSB;
using HSB.Constants;

Configuration c = new()
{
    Port = 8080,
    Address = ""
};

//expressjs-like routing


c.Get("/json", (Request req, Response res) =>
{
    Dictionary<string, string> obj = new()
    {
        { "key1", "value1" },
        { "key2", "value2" }
    };
    res.Json(obj);
});

//act as redirect

c.Get("/home", (Request req, Response res) =>
{
    res.Redirect("/");
});


c.Get("/404", (Request req, Response res) =>
{
    res.SendCode(404);
});


c.Get("/500", (Request req, Response res) =>
{
    res.SendCode(500);
});

c.Get("/error", (Request req, Response res) =>
{

    var errCode = req.Parameters["code"];

    if (errCode == null)
    {
        res.SendCode(400);
        return;
    }
    res.SendCode(int.Parse(errCode));
});



new Server(c).Start();