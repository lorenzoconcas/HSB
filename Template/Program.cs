using HSB;
Configuration c = new()
{
    port = 8080,
    address = "127.0.0.1",
    staticFolderPath = "./static"
};

new Server(c).Start();