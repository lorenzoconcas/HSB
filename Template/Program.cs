using HSB;
Configuration c = new()
{
    Port = 8080,
    Address = "127.0.0.1",
    StaticFolderPath = "./static"
};

new Server(c).Start();