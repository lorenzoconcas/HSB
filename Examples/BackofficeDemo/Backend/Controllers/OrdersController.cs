using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/orders")]
public sealed class OrdersController
{
    public Request req = null!;
    private Response res = null!;

    [Get("/")]
    [RequireAuth]
    public void List()
    {
        var page = QueryHelpers.ReadInt(req, "page", 1, 1, 1000);
        var pageSize = QueryHelpers.ReadInt(req, "pageSize", 12, 1, 100);
        var search = QueryHelpers.ReadString(req, "search");
        var status = QueryHelpers.ReadString(req, "status");

        res.SendJson(BackofficeApplication.Current.OrderService.List(search, status, page, pageSize));
    }

    [Get("/:id")]
    [RequireAuth]
    public void GetById([NamedParameter("id", true)] string id)
    {
        var order = BackofficeApplication.Current.OrderService.Get(id);
        if (order == null)
        {
            ApiResponses.NotFound(res, "Order not found.");
            return;
        }

        res.SendJson(order);
    }

    [Post("/")]
    [RequireAuth(Roles = ["admin", "manager", "operator"])]
    public void Create()
    {
        if (!RequestJson.TryRead<CreateOrderRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var (order, creationError) = BackofficeApplication.Current.OrderService.Create(request!, user);
        if (order == null)
        {
            ApiResponses.ValidationError(res, creationError ?? "Cannot create order.");
            return;
        }

        res.SendJson(order, 201);
    }

    [Put("/:id/status")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void UpdateStatus([NamedParameter("id", true)] string id)
    {
        if (!RequestJson.TryRead<UpdateOrderStatusRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.OrderService.UpdateStatus(id, request!.Status, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Order not found.");
            return;
        }

        res.SendJson(updated);
    }

    [Post("/:id/attachment")]
    [RequireAuth(Roles = ["admin", "manager", "operator"])]
    public void UploadAttachment([NamedParameter("id", true)] string id)
    {
        var formData = req.GetMultiPartFormData();
        var file = formData?.GetFiles().FirstOrDefault();
        if (file == null)
        {
            ApiResponses.ValidationError(res, "Multipart file is required.");
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.OrderService.AttachFile(id, file, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Order not found.");
            return;
        }

        res.SendJson(updated);
    }

    [Get("/export.csv")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void ExportCsv()
    {
        var csv = CsvWriter.WriteOrders(BackofficeApplication.Current.OrderService.ExportSnapshot());
        res.Send(csv, "text/csv; charset=utf-8", customHeaders: new Dictionary<string, string>
        {
            ["Content-Disposition"] = "attachment; filename=\"orders.csv\""
        });
    }
}
