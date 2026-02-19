using HSB;
Configuration c = new()
{
    Address = "127.0.0.1",
    StaticFolderPath = "./static"
};

new Server(c).Start();