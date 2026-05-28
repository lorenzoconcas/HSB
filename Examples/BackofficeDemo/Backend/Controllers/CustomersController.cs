using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Infrastructure;
using HSB;
using HSB.Components.Attributes;
using HSB.Components.Controller;
using HSB.Modules;

namespace BackofficeDemo.Backend.Controllers;

[Controller("/api/customers")]
public sealed class CustomersController
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

        res.SendJson(BackofficeApplication.Current.CustomerService.List(search, page, pageSize));
    }

    [Get("/:id")]
    [RequireAuth]
    public void GetById([NamedParameter("id", true)] string id)
    {
        var customer = BackofficeApplication.Current.CustomerService.Get(id);
        if (customer == null)
        {
            ApiResponses.NotFound(res, "Customer not found.");
            return;
        }

        res.SendJson(customer);
    }

    [Post("/")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void Create()
    {
        if (!RequestJson.TryRead<CreateCustomerRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        if (string.IsNullOrWhiteSpace(request!.Name))
        {
            ApiResponses.ValidationError(res, "Customer name is required.");
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        res.SendJson(BackofficeApplication.Current.CustomerService.Create(request, user), 201);
    }

    [Put("/:id")]
    [RequireAuth(Roles = ["admin", "manager"])]
    public void Update([NamedParameter("id", true)] string id)
    {
        if (!RequestJson.TryRead<UpdateCustomerRequest>(req, out var request, out var error))
        {
            ApiResponses.ValidationError(res, error);
            return;
        }

        var user = req.GetAuthContext()?.Username ?? "unknown";
        var updated = BackofficeApplication.Current.CustomerService.Update(id, request!, user);
        if (updated == null)
        {
            ApiResponses.NotFound(res, "Customer not found.");
            return;
        }

        res.SendJson(updated);
    }

    [Get("/:id/orders")]
    [RequireAuth]
    public void Orders([NamedParameter("id", true)] string id)
    {
        res.SendJson(BackofficeApplication.Current.OrderService.ByCustomer(id));
    }
}
