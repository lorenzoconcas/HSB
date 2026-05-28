using BackofficeDemo.Backend.Models;

namespace BackofficeDemo.Backend.Contracts.Responses;

public sealed class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
}

public sealed class DashboardSummaryResponse
{
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalOrders { get; set; }
    public int OrdersToday { get; set; }
    public int LowStockItems { get; set; }
    public decimal RevenueMonth { get; set; }
}

public sealed class MetricPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public sealed class CurrentUserResponse
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public Dictionary<string, string> Claims { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ActivityFeedResponse
{
    public List<AuditEvent> Items { get; set; } = [];
}
