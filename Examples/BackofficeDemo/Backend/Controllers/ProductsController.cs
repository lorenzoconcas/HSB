using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/products")]
public sealed class ProductsController
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
        var category = QueryHelpers.ReadString(req, "category");
        var lowStockOnly = QueryHelpers.ReadBool(req, "lowStockOnly");

        res.SendJson(new
        {
            categories = BackofficeApplication.Current.ProductService.Categories(),
            result = BackofficeApplication.Current.ProductService.List(search, category, lowStockOnly, page, pageSize)
        });
    }

    [Get("/:id")]
    [RequireAuth]
    public void GetById([NamedParameter("id", true)] string id)
    {
        var product = BackofficeApplication.Current.ProductService.Get(id);
        if (product == null)
        {
            ApiResponses.NotFound(res, "Product not found.");
            return;
        }

        res.SendJson(product);
    }

    [Post("/")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void Create()
    {
        if (!RequestJson.TryRead<CreateProductRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        if (string.IsNullOrWhiteSpace(request!.Name) || string.IsNullOrWhiteSpace(request.Sku))
        {
            ApiResponses.ValidationError(res, "Name and SKU are required.");
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        res.SendJson(BackofficeApplication.Current.ProductService.Create(request, user), 201);
    }

    [Put("/:id")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void Update([NamedParameter("id", true)] string id)
    {
        if (!RequestJson.TryRead<UpdateProductRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.ProductService.Update(id, request!, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Product not found.");
            return;
        }

        res.SendJson(updated);
    }

    [Patch("/:id/stock")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void AdjustStock([NamedParameter("id", true)] string id)
    {
        if (!RequestJson.TryRead<UpdateStockRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.ProductService.AdjustStock(id, request!.QuantityDelta, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Product not found.");
            return;
        }

        res.SendJson(updated);
    }

    [Post("/:id/image")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void UploadImage([NamedParameter("id", true)] string id)
    {
        var formData = req.GetMultiPartFormData();
        var file = formData?.GetFiles().FirstOrDefault();
        if (file == null)
        {
            ApiResponses.ValidationError(res, "Multipart file is required.");
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.ProductService.AttachImage(id, file, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Product not found.");
            return;
        }

        res.SendJson(updated);
    }
}
