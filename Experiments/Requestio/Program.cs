using HSB;
using HSB.Constants;

Configuration c = new()
{
    Port = 8080,
    Address = ""
};

//expressjs-like routing


c.GET("/json", (Request req, Response res) =>
{
    Dictionary<string, string> obj = new()
    {
        { "key1", "value1" },
        { "key2", "value2" }
    };
    res.JSON(obj);
});

//act as redirect

c.GET("/home", (Request req, Response res) =>
{
    res.Redirect("/");
});


c.GET("/404", (Request req, Response res) =>
{
    res.SendCode(404);
});


c.GET("/500", (Request req, Response res) =>
{
    res.SendCode(500);
});

c.GET("/error", (Request req, Response res) =>
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