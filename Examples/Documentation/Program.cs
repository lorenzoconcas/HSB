using HSB;

var c = new Configuration()
{
    Port = 8080,
    Address = "",
    DocumentationPath = "/documentation"
};

new Server(c).Start();
