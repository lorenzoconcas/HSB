using System.Collections.Concurrent;

namespace HSB;

internal sealed class RequestRateLimiter
{
    private readonly RateLimitOptions options;
    private readonly HashSet<string> ignoredIps;
    private readonly ConcurrentDictionary<string, ClientState> states = new(StringComparer.OrdinalIgnoreCase);
    private long lastTrimAt;

    public RequestRateLimiter(RateLimitOptions options)
    {
        this.options = options;
        ignoredIps = [.. options.IgnoredIps];
    }

    public RateLimitDecision Evaluate(string clientIp)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(clientIp) || ignoredIps.Contains(clientIp))
        {
            return RateLimitDecision.AllowedBypass;
        }

        var now = Environment.TickCount64;
        TrimStaleEntriesIfNeeded(now);

        var state = states.GetOrAdd(clientIp, _ => new ClientState(options.BurstLimit, now));
        lock (state.Sync)
        {
            Refill(state, now);

            if (state.BlockedUntilTick > now)
            {
                var retryAfterBlocked = GetRetryAfterSeconds(state.BlockedUntilTick - now);
                return new RateLimitDecision(false, options.BurstLimit, (int) Math.Floor(state.Tokens), retryAfterBlocked);
            }

            if (state.Tokens >= 1d)
            {
                state.Tokens -= 1d;
                state.LastSeenTick = now;

                var remaining = Math.Max(0, (int) Math.Floor(state.Tokens));
                var resetAfter = GetRetryAfterSeconds(GetMillisecondsUntilNextToken(state));
                return new RateLimitDecision(true, options.BurstLimit, remaining, resetAfter);
            }

            state.LastSeenTick = now;
            if (options.BlockDurationSeconds > 0)
            {
                state.BlockedUntilTick = now + (options.BlockDurationSeconds * 1000L);
            }

            var retryAfter = options.BlockDurationSeconds > 0
                ? options.BlockDurationSeconds
                : GetRetryAfterSeconds(GetMillisecondsUntilNextToken(state));
            return new RateLimitDecision(false, options.BurstLimit, 0, retryAfter);
        }
    }

    private void Refill(ClientState state, long now)
    {
        if (state.LastRefillTick >= now)
        {
            return;
        }

        var elapsedMilliseconds = now - state.LastRefillTick;
        var refillRate = (double) options.PermitLimit / (options.RefillPeriodSeconds * 1000d);
        state.Tokens = Math.Min(options.BurstLimit, state.Tokens + (elapsedMilliseconds * refillRate));
        state.LastRefillTick = now;
    }

    private long GetMillisecondsUntilNextToken(ClientState state)
    {
        if (state.Tokens >= 1d)
        {
            return 0;
        }

        var missingTokens = 1d - state.Tokens;
        var refillRate = (double) options.PermitLimit / (options.RefillPeriodSeconds * 1000d);
        if (refillRate <= 0d)
        {
            return options.RefillPeriodSeconds * 1000L;
        }

        return (long) Math.Ceiling(missingTokens / refillRate);
    }

    private void TrimStaleEntriesIfNeeded(long now)
    {
        if (states.Count <= options.MaxTrackedClients)
        {
            return;
        }

        var lastTrim = Interlocked.Read(ref lastTrimAt);
        if (now - lastTrim < 1000 || Interlocked.CompareExchange(ref lastTrimAt, now, lastTrim) != lastTrim)
        {
            return;
        }

        var staleAfter = Math.Max(options.RefillPeriodSeconds * 4L * 1000L, 60000L);
        foreach (var entry in states)
        {
            var state = entry.Value;
            lock (state.Sync)
            {
                if (now - state.LastSeenTick < staleAfter || state.BlockedUntilTick > now)
                {
                    continue;
                }
            }

            states.TryRemove(entry.Key, out _);
        }
    }

    private static int GetRetryAfterSeconds(long milliseconds)
    {
        return milliseconds <= 0 ? 0 : (int) Math.Ceiling(milliseconds / 1000d);
    }

    private sealed class ClientState
    {
        public ClientState(double tokens, long now)
        {
            Tokens = tokens;
            LastRefillTick = now;
            LastSeenTick = now;
        }

        public object Sync { get; } = new();
        public double Tokens { get; set; }
        public long LastRefillTick { get; set; }
        public long LastSeenTick { get; set; }
        public long BlockedUntilTick { get; set; }
    }
}

internal readonly record struct RateLimitDecision(bool Allowed, int Limit, int Remaining, int RetryAfterSeconds)
{
    public static RateLimitDecision AllowedBypass { get; } = new(true, 0, 0, 0);
}
