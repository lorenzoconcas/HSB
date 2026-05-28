using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/dashboard")]
public sealed class DashboardController
{
    private Response res = null!;

    [Get("/summary")]
    [RequireAuth]
    public void Summary()
    {
        res.SendJson(BackofficeApplication.Current.DashboardService.GetSummary());
    }

    [Get("/sales")]
    [RequireAuth]
    public void Sales()
    {
        res.SendJson(BackofficeApplication.Current.DashboardService.GetSalesSeries());
    }

    [Get("/low-stock")]
    [RequireAuth]
    public void LowStock()
    {
        res.SendJson(BackofficeApplication.Current.DashboardService.GetLowStock());
    }

    [Get("/recent-orders")]
    [RequireAuth]
    public void RecentOrders()
    {
        res.SendJson(BackofficeApplication.Current.DashboardService.GetRecentOrders());
    }

    [Get("/activity")]
    [RequireAuth]
    public void Activity()
    {
        res.SendJson(new
        {
            items = BackofficeApplication.Current.ActivityService.List()
        });
    }
}
