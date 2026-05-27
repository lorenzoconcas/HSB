using System.Text.Json;
using HSB;
using HSB.OpenApi;
using HSB.OpenApi.Attributes;

namespace StressTest;

internal static class Program
{
    private static readonly Random random = new();

    [ApiTag("Orders")]
    [ApiDescription("Simulates an order elaboration request")]
    [ApiSummary("Simulates an order elaboration request")]
    private static void GetOrders(Request req, Response res)
    {
        List<object> orders = [];

        for (var i = 0; i < 50000; i++)
        {
            orders.Add(new
            {
                id = Guid.NewGuid(),
               // amount = random.Next(10, 10000),
                currency = "EUR",
                createdAt = DateTime.UtcNow
            });
        }

        res.Send(
            JsonSerializer.Serialize(orders),
            "application/json"
        );
    }

    [ApiTag("Health")]
    [ApiDescription("Simulates an health check")]
    private static void GetHealth(Request req, Response res)
    {
        res.Send(
            JsonSerializer.Serialize(new
            {
                status = "ok",
                uptime = Environment.TickCount64,
                version = "2.4.1",
                timestamp = DateTime.UtcNow
            }),
            "application/json"
        );
    }

    [ApiTag("Auth")]
    [ApiDescription("Simulates a login")]
    private static void Login(Request req, Response res)
    {
        if (req.Body.Contains("admin"))
        {
            res.Send(
                JsonSerializer.Serialize(new
                {
                    accessToken = Guid.NewGuid(),
                    refreshToken = Guid.NewGuid(),
                    expiresIn = 3600,
                    user = new
                    {
                        id = "usr_admin",
                        role = "admin"
                    }
                }),
                "application/json"
            );

            return;
        }

        res.Send(
            JsonSerializer.Serialize(new
            {
                error = "invalid_credentials"
            }),
            "application/json",
            401
        );
    }

    [ApiTag("Users")]
    [ApiDescription("Simulates user listing")]
    private static void GetUsers(Request req, Response res)
    {
        List<object> users = [];

        for (int i = 0; i < 100; i++)
        {
            users.Add(new
            {
                id = $"usr_{i}",
                email = $"user{i}@company.local",
                active = random.NextDouble() > 0.3,
                createdAt = DateTime.UtcNow.AddDays(-random.Next(0, 365))
            });
        }

        res.Send(
            JsonSerializer.Serialize(users),
            "application/json"
        );
    }

    [ApiTag("Reports")]
    [ApiDescription("Simulates a slow endpoint with rep")]
    private static async Task GetSlowReport(Request req, Response res)
    {
        await Task.Delay(random.Next(2000, 8000));

        res.Send(
            JsonSerializer.Serialize(new
            {
                generated = true,
                rows = random.Next(1000, 500000)
            }),
            "application/json"
        );
    }

    [ApiTag("Diagnostics")]
    private static void GetRandomError(Request req, Response res)
    {
        if (random.NextDouble() > 0.5)
        {
            res.Send(
                JsonSerializer.Serialize(new
                {
                    error = "database_timeout",
                    retryable = true
                }),
                "application/json",
                500
            );

            return;
        }

        res.Send(
            JsonSerializer.Serialize(new
            {
                success = true
            }),
            "application/json"
        );
    }

    [ApiTag("Orders")]
    private static void GetSmallOrders(Request req, Response res)
    {
        List<object> orders = [];

        for (var i = 0; i < 100; i++)
        {
            orders.Add(new
            {
                id = Guid.NewGuid(),
                amount = random.Next(10, 10000),
                currency = "EUR",
                createdAt = DateTime.UtcNow
            });
        }

        res.Send(
            JsonSerializer.Serialize(orders),
            "application/json"
        );
    }

    [ApiTag("Diagnostics")]
    private static void CpuBurn(Request req, Response res)
    {
        double x = 0;

        for (int i = 0; i < 100000000; i++)
        {
            x += Math.Sqrt(i);
        }

        res.Send("done");
    }

    [ApiTag("Upload")]
    private static void Upload(Request req, Response res)
    {
        var bodySize = req.Body?.Length ?? 0;

        res.Send(
            JsonSerializer.Serialize(new
            {
                success = true,
                receivedBytes = bodySize,
                timestamp = DateTime.UtcNow
            }),
            "application/json"
        );
    }

    private static void Main(string[] args)
    {
        Configuration config = new()
        {
            Port = 8080,
            RequestMaxSize = 100 * HSB.Configuration.MEGABYTE,
            Http =
            {
                MaxBodySizeBytes = 2L * 1024 * 1024 * 1024
            },
            Upload =
            {
                MaxConcurrentUploads = 10,
                TempPath = "./temp"
            },
            //Address = "0.0.0.0"
        };

        // Healthcheck

        config.Get("/health", GetHealth);

        // Fake login
        config.Post("/auth/login", Login);

        // Lista utenti
        config.Get("/users", GetUsers);

        // Endpoint lento
        config.Get("/slow-report", GetSlowReport);

        // Errori randomici
        config.Get("/random-error", GetRandomError);

        // Payload enorme per benchmark
        config.Get("/orders", GetOrders);


        config.Get("/orders-small", GetSmallOrders);

        config.Get("/cpu-burn", CpuBurn);

        config.Post("/upload", Upload);


        WebSocketHandler.Register(config);

        Server server = new(config);

        server.Start();
    }
}