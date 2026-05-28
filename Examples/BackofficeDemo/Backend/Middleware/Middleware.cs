using System.Collections.Concurrent;
using System.Diagnostics;
using HSB;

namespace BackofficeDemo.Backend.Middleware;

public static class RequestIdMiddleware
{
    public static async ValueTask InvokeAsync(RequestContext context, MiddlewareNext next)
    {
        var requestId = Guid.NewGuid().ToString("N");
        context.Request.SetItem("requestId", requestId);
        context.Response.SetHeader("X-Request-Id", requestId);
        await next();
    }
}

public static class LoginRateLimitMiddleware
{
    private static readonly ConcurrentDictionary<string, LoginAttemptWindow> Attempts = new(StringComparer.OrdinalIgnoreCase);

    public static ValueTask InvokeAsync(RequestContext context, MiddlewareNext next)
    {
        if (!context.Request.Path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            !context.Request.Method.ToString().Equals("Post", StringComparison.OrdinalIgnoreCase))
        {
            return next();
        }

        var now = DateTime.UtcNow;
        var window = Attempts.GetOrAdd(context.Request.ClientIp, _ => new LoginAttemptWindow());
        lock (window)
        {
            if ((now - window.WindowStartUtc) > TimeSpan.FromMinutes(1))
            {
                window.WindowStartUtc = now;
                window.Attempts = 0;
            }

            window.Attempts++;
            if (window.Attempts > 10)
            {
                context.Response.SetHeader("Retry-After", "60");
                context.Response.Json(new
                {
                    error = "TooManyLoginAttempts"
                }, 429);
                return ValueTask.CompletedTask;
            }
        }

        return next();
    }

    private sealed class LoginAttemptWindow
    {
        public DateTime WindowStartUtc { get; set; } = DateTime.UtcNow;
        public int Attempts { get; set; }
    }
}

public static class RequestLoggingMiddleware
{
    public static async ValueTask InvokeAsync(RequestContext context, MiddlewareNext next)
    {
        context.Configuration.Debug.INFO(
            $"[REQ] {context.Request.Method} {context.Request.Path} ip={context.Request.ClientIp}");
        await next();
    }
}

public static class TimingMiddleware
{
    public static async ValueTask InvokeAsync(RequestContext context, MiddlewareNext next)
    {
        var stopwatch = Stopwatch.StartNew();
        await next();
        stopwatch.Stop();
        context.Response.SetHeader("X-Response-Time-Ms", stopwatch.ElapsedMilliseconds.ToString());
    }
}
