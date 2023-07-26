using HSB;

Configuration c = new()
{
    port = 8080, //you must be root to listen on port 80, so 8080 will be used instead (see http alternate port)
    address = "" //with empty string the server will still listen to any address 
};

//expressjs-like routing


c.GET("/", (Request req, Response res) =>
{
    //reply to request with an hello world
    res.SendHTMLContent("<h1>Hello world</h1>");
});


new Server(c).Start(true);