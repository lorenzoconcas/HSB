using HSB;
Configuration c = new()
{
    Address = "127.0.0.1",
    StaticFolderPath = "./static"
};

c.Get("/", (Response res) =>
{
    res.SendHtmlContent("<h1>Hello from HSB</h1>");
});

new Server(c).Start();
