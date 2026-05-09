using HSB;
using HSB.OpenApi;
using HSB.OpenApi.models;
using Server = HSB.Server;

var c = new Configuration
{
    OpenApiSettings = new OpenApiSettings()
    {
        Mode = Mode.Full,
        Path = "/swagger/index.html",
        Info = new Info("Controller Example", "An example of a controller")
    },
    GlobalCors = new Cors()
    {
        AllowedOrigins = ["*"]
    }
};

new Server(c).Start();