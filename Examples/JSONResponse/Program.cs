using HSB;
using HSB.Constants;

Configuration c = new()
{
    Port = 8080,
    Address = ""
};

//expressjs-like routing


c.Get("/", (Request req, Response res) =>
{
    Dictionary<string, string> obj = new()
    {
        { "key1", "value1" },
        { "key2", "value2" }
    };
    res.Json(obj);
});



new Server(c).Start();