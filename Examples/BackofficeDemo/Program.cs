using BackofficeDemo.Backend.Infrastructure;
using BackofficeDemo.Backend.Middleware;
using BackofficeDemo.Backend.WebSockets;
using HSB;
using HSB.Components.WebSockets;
using HSB.OpenApi;
using Info = HSB.OpenApi.models.Info;

var projectRoot = ProjectPaths.ResolveProjectRoot();
var staticRoot = Path.Combine(projectRoot, "frontend", "dist");

var configuration = new Configuration
{
    Port = 5098,
    StaticFolderPath = staticRoot,
    GlobalCors = new Cors
    {
        AllowedOrigins = ["*"]
    },
    OpenApiSettings = new OpenApiSettings
    {
        Mode = Mode.Full,
        Path = "/swagger/index.html",
        Info = new Info("HSB Backoffice Demo", "A realistic backoffice demo built on HSB.")
    }
};

configuration.Security.Headers.Enabled = true;
configuration.Security.Headers.AddPermissionsPolicyHeader = true;
configuration.Security.Validation.Enabled = true;
configuration.Security.Validation.AllowedHosts = ["localhost", "127.0.0.1"];
configuration.Security.RateLimit.Enabled = true;
configuration.Security.RateLimit.PermitLimit = 240;
configuration.Security.RateLimit.BurstLimit = 300;
configuration.Security.RateLimit.RefillPeriodSeconds = 60;
configuration.Security.RateLimit.BlockDurationSeconds = 30;

BackofficeApplication.Initialize(projectRoot, staticRoot);

configuration.Use(RequestIdMiddleware.InvokeAsync);
configuration.Use(LoginRateLimitMiddleware.InvokeAsync);
configuration.Use(RequestLoggingMiddleware.InvokeAsync);
configuration.Use(TimingMiddleware.InvokeAsync);

configuration.Get("/api/health", (Response response) =>
{
    response.SendJson(new
    {
        status = "ok",
        service = "BackofficeDemo",
        timestampUtc = DateTime.UtcNow
    });
});

configuration.Get("/api/meta", (Response response) =>
{
    response.SendJson(new
    {
        app = "BackofficeDemo",
        version = "0.0.22",
        websocket = "/ws/notifications",
        swagger = "/swagger/index.html"
    });
});

configuration.WebSocket("/ws/notifications", BackofficeNotificationsHub.ConfigureAsync);

new Server(configuration).Start();
