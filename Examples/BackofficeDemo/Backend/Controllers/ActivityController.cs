using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/activity")]
public sealed class ActivityController
{
    private Response res = null!;

    [Get("/")]
    [RequireAuth]
    public void List()
    {
        res.SendJson(new
        {
            items = BackofficeApplication.Current.ActivityService.List(60)
        });
    }
}
