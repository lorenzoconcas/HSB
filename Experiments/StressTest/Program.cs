using System.Text.Json;
using HSB;

namespace StressTest;

public static class Program
{
    private static readonly Random Random = new();

    public static void Main()
    {
        Configuration config = new()
        {
            Port = 8080,
            RequestMaxSize = 100 * HSB.Configuration.MEGABYTE
            //Address = "0.0.0.0"
        };
        config.Http.MaxBodySizeBytes = 2L * 1024 * 1024 * 1024;
        config.Upload.MaxConcurrentUploads = 10;
        config.Upload.TempPath = "./temp";

        // Healthcheck
        config.Get("/health", (Request req, Response res) =>
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
        });

        // Fake login
        config.Post("/auth/login", (Request req, Response res) =>
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
        });

        // Lista utenti
        config.Get("/users", (Request req, Response res) =>
        {
            List<object> users = [];

            for (int i = 0; i < 100; i++)
            {
                users.Add(new
                {
                    id = $"usr_{i}",
                    email = $"user{i}@company.local",
                    active = Random.NextDouble() > 0.3,
                    createdAt = DateTime.UtcNow.AddDays(-Random.Next(0, 365))
                });
            }

            res.Send(
                JsonSerializer.Serialize(users),
                "application/json"
            );
        });

        // Endpoint lento
        config.Get("/slow-report", async (Request req, Response res) =>
        {
            await Task.Delay(Random.Next(2000, 8000));

            res.Send(
                JsonSerializer.Serialize(new
                {
                    generated = true,
                    rows = Random.Next(1000, 500000)
                }),
                "application/json"
            );
        });

        // Errori randomici
        config.Get("/random-error", (Request req, Response res) =>
        {
            if (Random.NextDouble() > 0.5)
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
        });

        // Payload enorme per benchmark
        config.Get("/orders", (Request req, Response res) =>
        {
            List<object> orders = [];

            for (var i = 0; i < 50000; i++)
            {
                orders.Add(new
                {
                    id = Guid.NewGuid(),
                    amount = Random.Next(10, 10000),
                    currency = "EUR",
                    createdAt = DateTime.UtcNow
                });
            }

            res.Send(
                JsonSerializer.Serialize(orders),
                "application/json"
            );
        });
        
        
        
        
        config.Get("/orders-small", (Request req, Response res) =>
        {
            List<object> orders = [];

            for (var i = 0; i < 100; i++)
            {
                orders.Add(new
                {
                    id = Guid.NewGuid(),
                    amount = Random.Next(10, 10000),
                    currency = "EUR",
                    createdAt = DateTime.UtcNow
                });
            }

            res.Send(
                JsonSerializer.Serialize(orders),
                "application/json"
            );
        });

        config.Get("/cpu-burn", (Request req, Response res) =>
        {
            double x = 0;

            for(int i = 0; i < 100000000; i++)
            {
                x += Math.Sqrt(i);
            }

            res.Send("done");
        });

        config.Post("/upload", (Request req, Response res) =>
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
        });

        
        
        WebSocketHandler.Register(config);

        Server server = new(config);

        Console.WriteLine("Fake enterprise API running on :8080");

        server.Start();
    }
}
