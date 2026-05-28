using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BackofficeDemo.Backend.Models;
using BackofficeDemo.Backend.Services;
using HSB;

namespace BackofficeDemo.Backend.Infrastructure;

public static class ProjectPaths
{
    public static string ResolveProjectRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BackofficeDemo.csproj")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Cannot locate BackofficeDemo project root.");
    }
}

public static class IdGenerator
{
    public static string EntityId(string prefix)
    {
        return $"{prefix}_{Convert.ToHexString(RandomNumberGenerator.GetBytes(5)).ToLowerInvariant()}";
    }

    public static string OrderNumber()
    {
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}";
    }

    public static string Token()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
    }
}

public static class RequestJson
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryRead<T>(Request request, out T? model, out string error)
    {
        model = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            error = "Request body is required";
            return false;
        }

        try
        {
            model = JsonSerializer.Deserialize<T>(request.Body, JsonOptions);
            if (model == null)
            {
                error = "Request body is invalid";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}

public static class QueryHelpers
{
    public static int ReadInt(Request request, string key, int fallback, int min = 1, int max = 500)
    {
        if (!request.Parameters.TryGetValue(key, out var value) || !int.TryParse(value, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, min, max);
    }

    public static string ReadString(Request request, string key)
    {
        return request.Parameters.TryGetValue(key, out var value) ? value.Trim() : string.Empty;
    }

    public static bool ReadBool(Request request, string key)
    {
        return request.Parameters.TryGetValue(key, out var value) &&
               (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
    }
}

public static class ApiResponses
{
    public static void ValidationError(Response response, string message)
    {
        response.Json(new
        {
            error = "ValidationError",
            message
        }, 400);
    }

    public static void NotFound(Response response, string message)
    {
        response.Json(new
        {
            error = "NotFound",
            message
        }, 404);
    }
}

public static class CsvWriter
{
    public static string WriteOrders(IEnumerable<OrderRecord> orders)
    {
        var builder = new StringBuilder();
        builder.AppendLine("OrderNumber,Customer,Status,CreatedAtUtc,CreatedBy,Subtotal,Discount,Total,Items");

        foreach (var order in orders)
        {
            builder.AppendLine(string.Join(",",
                Escape(order.OrderNumber),
                Escape(order.CustomerName),
                Escape(order.Status.ToString()),
                Escape(order.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture)),
                Escape(order.CreatedBy),
                Escape(order.Subtotal.ToString(CultureInfo.InvariantCulture)),
                Escape(order.Discount.ToString(CultureInfo.InvariantCulture)),
                Escape(order.Total.ToString(CultureInfo.InvariantCulture)),
                Escape(string.Join(" | ", order.Items.Select(item => $"{item.ProductName} x{item.Quantity}")))));
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}

public sealed class BackofficeApplication
{
    private BackofficeApplication(string projectRoot, string staticRoot)
    {
        ProjectRoot = projectRoot;
        StaticRoot = staticRoot;
        UploadRoot = Path.Combine(staticRoot, "uploads");
        ProductsUploadRoot = Path.Combine(UploadRoot, "products");
        OrdersUploadRoot = Path.Combine(UploadRoot, "orders");

        Directory.CreateDirectory(StaticRoot);
        Directory.CreateDirectory(UploadRoot);
        Directory.CreateDirectory(ProductsUploadRoot);
        Directory.CreateDirectory(OrdersUploadRoot);

        State = new BackofficeState();
        ActivityService = new ActivityService(State);
        AuthService = new AuthService(State);
        ProductService = new ProductService(State, ActivityService, ProductsUploadRoot);
        CustomerService = new CustomerService(State, ActivityService);
        OrderService = new OrderService(State, ActivityService, OrdersUploadRoot);
        InventoryService = new InventoryService(State, ActivityService);
        DashboardService = new DashboardService(State);
    }

    public static BackofficeApplication Current { get; private set; } = null!;

    public string ProjectRoot { get; }
    public string StaticRoot { get; }
    public string UploadRoot { get; }
    public string ProductsUploadRoot { get; }
    public string OrdersUploadRoot { get; }
    public BackofficeState State { get; }
    public AuthService AuthService { get; }
    public ProductService ProductService { get; }
    public CustomerService CustomerService { get; }
    public OrderService OrderService { get; }
    public InventoryService InventoryService { get; }
    public DashboardService DashboardService { get; }
    public ActivityService ActivityService { get; }

    public static BackofficeApplication Initialize(string projectRoot, string staticRoot)
    {
        Current = new BackofficeApplication(projectRoot, staticRoot);
        Seeder.Seed(Current);
        return Current;
    }
}

public static class Seeder
{
    public static void Seed(BackofficeApplication app)
    {
        SeedUsers(app.State);
        SeedCatalog(app.State);
        SeedCustomers(app.State);
        SeedOrdersAndInventory(app.State);
    }

    private static void SeedUsers(BackofficeState state)
    {
        state.Users.AddRange(
        [
            new UserAccount
            {
                Id = IdGenerator.EntityId("usr"),
                Username = "admin",
                Password = "admin123",
                FullName = "Alice Admin",
                Roles = ["admin"]
            },
            new UserAccount
            {
                Id = IdGenerator.EntityId("usr"),
                Username = "manager",
                Password = "manager123",
                FullName = "Marco Manager",
                Roles = ["manager"]
            },
            new UserAccount
            {
                Id = IdGenerator.EntityId("usr"),
                Username = "operator",
                Password = "operator123",
                FullName = "Olivia Operator",
                Roles = ["operator"]
            }
        ]);
    }

    private static void SeedCatalog(BackofficeState state)
    {
        var now = DateTime.UtcNow;
        state.Products.AddRange(
        [
            CreateProduct("SKU-1001", "Desktop Workstation", "Hardware", 1499m, 6, 4, now),
            CreateProduct("SKU-1002", "27\" Monitor", "Hardware", 289m, 14, 5, now),
            CreateProduct("SKU-1003", "Wireless Keyboard", "Accessories", 69m, 24, 8, now),
            CreateProduct("SKU-1004", "USB-C Dock", "Accessories", 119m, 8, 6, now),
            CreateProduct("SKU-1005", "Ergonomic Chair", "Furniture", 399m, 3, 4, now),
            CreateProduct("SKU-1006", "Network Switch", "Networking", 239m, 11, 3, now),
            CreateProduct("SKU-1007", "Thermal Label Printer", "Operations", 179m, 5, 5, now),
            CreateProduct("SKU-1008", "Barcode Scanner", "Operations", 129m, 18, 6, now),
            CreateProduct("SKU-1009", "Meeting Room Camera", "AV", 549m, 4, 2, now),
            CreateProduct("SKU-1010", "Laptop Sleeve", "Accessories", 39m, 30, 10, now),
            CreateProduct("SKU-1011", "Noise Cancelling Headset", "AV", 219m, 9, 4, now),
            CreateProduct("SKU-1012", "Spare SSD 2TB", "Hardware", 159m, 7, 5, now)
        ]);
    }

    private static void SeedCustomers(BackofficeState state)
    {
        var now = DateTime.UtcNow;
        state.Customers.AddRange(
        [
            CreateCustomer("Northwind Labs", "northwind@example.com", "Rome", now),
            CreateCustomer("Blue Harbor SRL", "blueharbor@example.com", "Milan", now),
            CreateCustomer("Pixel Forge Studio", "pixelforge@example.com", "Turin", now),
            CreateCustomer("Cedar Peak Retail", "cedarpeak@example.com", "Bologna", now),
            CreateCustomer("Aster Logistics", "aster@example.com", "Naples", now),
            CreateCustomer("Nova Clinic Group", "nova@example.com", "Florence", now),
            CreateCustomer("Orchid Consulting", "orchid@example.com", "Verona", now),
            CreateCustomer("Delta Office Hub", "delta@example.com", "Parma", now)
        ]);
    }

    private static void SeedOrdersAndInventory(BackofficeState state)
    {
        if (state.Products.Count < 4 || state.Customers.Count < 3)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var order1 = CreateOrder(state, state.Customers[0], "admin", now.AddDays(-4),
            (state.Products[1], 4),
            (state.Products[2], 8));
        var order2 = CreateOrder(state, state.Customers[2], "manager", now.AddDays(-2),
            (state.Products[0], 1),
            (state.Products[10], 3));
        var order3 = CreateOrder(state, state.Customers[4], "operator", now.AddHours(-9),
            (state.Products[6], 2),
            (state.Products[7], 2),
            (state.Products[9], 10));

        state.Orders.AddRange([order1, order2, order3]);
        state.InventoryAdjustments.AddRange(
        [
            CreateAdjustment(state.Products[4], InventoryAdjustmentType.Restock, 6, "Initial showroom refill", "admin", now.AddDays(-5)),
            CreateAdjustment(state.Products[8], InventoryAdjustmentType.Correction, -1, "Damaged box", "manager", now.AddDays(-1)),
            CreateAdjustment(state.Products[11], InventoryAdjustmentType.Restock, 10, "Supplier replenishment", "manager", now.AddHours(-7))
        ]);

        state.AuditEvents.AddRange(
        [
            CreateAudit("system.seeded", "Demo data initialized", "Backoffice demo datasets are ready.", "system", now.AddMinutes(-30)),
            CreateAudit("order.created", $"Order {order1.OrderNumber}", "Seed order created for Northwind Labs.", "admin", order1.CreatedAtUtc),
            CreateAudit("order.created", $"Order {order2.OrderNumber}", "Seed order created for Pixel Forge Studio.", "manager", order2.CreatedAtUtc),
            CreateAudit("inventory.adjusted", state.Products[11].Name, "Inventory replenished by 10 units.", "manager", now.AddHours(-7))
        ]);
    }

    private static Product CreateProduct(string sku, string name, string category, decimal price, int stock, int reorder,
        DateTime now)
    {
        return new Product
        {
            Id = IdGenerator.EntityId("prd"),
            Sku = sku,
            Name = name,
            Description = $"{name} ready for operational backoffice flows.",
            Category = category,
            Price = price,
            StockQuantity = stock,
            ReorderLevel = reorder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private static Customer CreateCustomer(string name, string email, string city, DateTime now)
    {
        return new Customer
        {
            Id = IdGenerator.EntityId("cus"),
            Code = $"C-{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}",
            Name = name,
            Email = email,
            Phone = "+39 06 5555 0000",
            VatNumber = $"IT{Convert.ToHexString(RandomNumberGenerator.GetBytes(4))}",
            City = city,
            Country = "Italy",
            Notes = "Seeded customer record",
            CreatedAtUtc = now
        };
    }

    private static OrderRecord CreateOrder(BackofficeState state, Customer customer, string createdBy, DateTime createdAtUtc,
        params (Product product, int quantity)[] items)
    {
        var orderItems = items.Select(item => new OrderItem
        {
            ProductId = item.product.Id,
            ProductName = item.product.Name,
            Quantity = item.quantity,
            UnitPrice = item.product.Price,
            LineTotal = item.product.Price * item.quantity
        }).ToList();

        foreach (var item in items)
        {
            item.product.StockQuantity = Math.Max(0, item.product.StockQuantity - item.quantity);
            item.product.UpdatedAtUtc = createdAtUtc;
        }

        var subtotal = orderItems.Sum(item => item.LineTotal);
        return new OrderRecord
        {
            Id = IdGenerator.EntityId("ord"),
            OrderNumber = IdGenerator.OrderNumber(),
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            Status = OrderStatus.Confirmed,
            CreatedAtUtc = createdAtUtc,
            CreatedBy = createdBy,
            Items = orderItems,
            Subtotal = subtotal,
            Discount = 0,
            Total = subtotal,
            Notes = "Seeded order"
        };
    }

    private static InventoryAdjustment CreateAdjustment(Product product, InventoryAdjustmentType type, int delta, string reason,
        string createdBy, DateTime createdAtUtc)
    {
        product.StockQuantity += delta;
        product.UpdatedAtUtc = createdAtUtc;

        return new InventoryAdjustment
        {
            Id = IdGenerator.EntityId("adj"),
            ProductId = product.Id,
            ProductName = product.Name,
            Type = type,
            QuantityDelta = delta,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc
        };
    }

    private static AuditEvent CreateAudit(string type, string title, string description, string createdBy, DateTime createdAtUtc)
    {
        return new AuditEvent
        {
            Id = IdGenerator.EntityId("evt"),
            Type = type,
            Title = title,
            Description = description,
            CreatedBy = createdBy,
            CreatedAtUtc = createdAtUtc
        };
    }
}
