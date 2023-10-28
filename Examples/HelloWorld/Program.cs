//this is the example shown on www.lorenzoconcas.github.io/HSB

using HSB;

Configuration c = new()
{
    Port = 8080, //you must be root to listen on port 80, so 8080 will be used instead (see http alternate port)
    Address = "" //with empty string the server will still listen to any address 
};

//expressjs-like routing


c.GET("/", (Request req, Response res) =>
{
    //reply to request with an hello world
    res.SendHTMLContent("<h1>Hello world</h1>");
});


new Server(c).Start(true);