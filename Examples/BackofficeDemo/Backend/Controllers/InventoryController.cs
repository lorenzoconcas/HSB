using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/inventory")]
public sealed class InventoryController
{
    public Request req = null!;
    private Response res = null!;

    [Get("/adjustments")]
    [RequireAuth]
    public void Adjustments()
    {
        res.SendJson(BackofficeApplication.Current.InventoryService.List());
    }

    [Get("/low-stock")]
    [RequireAuth]
    public void LowStock()
    {
        res.SendJson(BackofficeApplication.Current.InventoryService.LowStock());
    }

    [Post("/adjustments")]
    [RequireAuth(Roles = ["admin", "manager", "operator"])]
    public void CreateAdjustment()
    {
        if (!RequestJson.TryRead<CreateInventoryAdjustmentRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        if (request!.Type == Models.InventoryAdjustmentType.ManualDecrease &&
            req.GetAuthContext()?.Roles.Contains("operator", StringComparer.OrdinalIgnoreCase) == true &&
            request.QuantityDelta < -5)
        {
            res.Json(new
            {
                error = "Forbidden",
                message = "Operators cannot decrease more than 5 units in a single adjustment."
            }, 403);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var (adjustment, creationError) = BackofficeApplication.Current.InventoryService.Create(request, user);
        if (adjustment == null)
        {
            ApiResponses.ValidationError(res, creationError ?? "Cannot create adjustment.");
            return;
        }

        res.SendJson(adjustment, 201);
    }
}
