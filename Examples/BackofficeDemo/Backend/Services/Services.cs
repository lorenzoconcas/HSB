using System.Security.Cryptography;
using System.Text;
using BackofficeDemo.Backend.Contracts.Requests;
using BackofficeDemo.Backend.Contracts.Responses;
using BackofficeDemo.Backend.Infrastructure;
using BackofficeDemo.Backend.Models;
using HSB.Components;
using HSB.Modules;

namespace BackofficeDemo.Backend.Services;

public sealed class ActivityService(BackofficeState state)
{
    public AuditEvent Record(string type, string title, string description, string createdBy)
    {
        AuditEvent audit;
        lock (state.SyncRoot)
        {
            audit = new AuditEvent
            {
                Id = IdGenerator.EntityId("evt"),
                Type = type,
                Title = title,
                Description = description,
                CreatedBy = createdBy,
                CreatedAtUtc = DateTime.UtcNow
            };
            state.AuditEvents.Insert(0, audit);
            if (state.AuditEvents.Count > 200)
            {
                state.AuditEvents.RemoveRange(200, state.AuditEvents.Count - 200);
            }
        }

        _ = WebSockets.BackofficeNotificationsHub.BroadcastAsync(new NotificationEnvelope
        {
            Type = type,
            TimestampUtc = audit.CreatedAtUtc,
            Payload = audit
        });

        return audit;
    }

    public List<AuditEvent> List(int take = 30)
    {
        lock (state.SyncRoot)
        {
            return state.AuditEvents
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(take)
                .Select(item => new AuditEvent
                {
                    Id = item.Id,
                    Type = item.Type,
                    Title = item.Title,
                    Description = item.Description,
                    CreatedBy = item.CreatedBy,
                    CreatedAtUtc = item.CreatedAtUtc
                })
                .ToList();
        }
    }
}

public sealed class AuthService(BackofficeState state)
{
    private readonly Dictionary<string, LoginResponse> activeTokens = new(StringComparer.Ordinal);
    private readonly object sync = new();

    public LoginResponse? Login(string username, string password)
    {
        var user = state.Users.FirstOrDefault(item => item.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null || !FixedTimeEquals(user.Password, password))
        {
            return null;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;

        var token = IdGenerator.Token();
        var response = new LoginResponse
        {
            AccessToken = token,
            Username = user.Username,
            FullName = user.FullName,
            Roles = [.. user.Roles]
        };

        AuthenticationManager.Instance.AddBearerToken(token, user.Username, user.Roles, new Dictionary<string, string>
        {
            ["fullName"] = user.FullName
        });

        lock (sync)
        {
            activeTokens[token] = response;
        }

        return response;
    }

    public void Logout(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        AuthenticationManager.Instance.RemoveBearerToken(token);
        lock (sync)
        {
            activeTokens.Remove(token);
        }
    }

    public LoginResponse? GetSession(string token)
    {
        lock (sync)
        {
            return activeTokens.TryGetValue(token, out var session)
                ? new LoginResponse
                {
                    AccessToken = session.AccessToken,
                    Username = session.Username,
                    FullName = session.FullName,
                    Roles = [.. session.Roles]
                }
                : null;
        }
    }

    public CurrentUserResponse? GetCurrentUser(AuthContext? authContext)
    {
        if (authContext == null)
        {
            return null;
        }

        return new CurrentUserResponse
        {
            Username = authContext.Username ?? string.Empty,
            FullName = authContext.Claims.TryGetValue("fullName", out var fullName) ? fullName : authContext.Username ?? string.Empty,
            Roles = [.. authContext.Roles],
            Claims = new Dictionary<string, string>(authContext.Claims, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

public sealed class ProductService(BackofficeState state, ActivityService activityService, string uploadRoot)
{
    public PagedResult<Product> List(string search, string category, bool lowStockOnly, int page, int pageSize)
    {
        lock (state.SyncRoot)
        {
            IEnumerable<Product> query = state.Products;
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item =>
                    item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Sku.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (lowStockOnly)
            {
                query = query.Where(item => item.StockQuantity <= item.ReorderLevel);
            }

            var totalCount = query.Count();
            var items = query
                .OrderBy(item => item.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneProduct)
                .ToList();

            return new PagedResult<Product>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }

    public Product? Get(string id)
    {
        lock (state.SyncRoot)
        {
            return state.Products.Where(item => item.Id == id).Select(CloneProduct).FirstOrDefault();
        }
    }

    public List<string> Categories()
    {
        lock (state.SyncRoot)
        {
            return state.Products.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).Order().ToList();
        }
    }

    public Product Create(CreateProductRequest request, string createdBy)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = IdGenerator.EntityId("prd"),
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            ReorderLevel = request.ReorderLevel,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        lock (state.SyncRoot)
        {
            state.Products.Add(product);
        }

        activityService.Record("product.created", product.Name, $"Product {product.Sku} created.", createdBy);
        return CloneProduct(product);
    }

    public Product? Update(string id, UpdateProductRequest request, string updatedBy)
    {
        lock (state.SyncRoot)
        {
            var product = state.Products.FirstOrDefault(item => item.Id == id);
            if (product == null)
            {
                return null;
            }

            product.Sku = request.Sku.Trim();
            product.Name = request.Name.Trim();
            product.Description = request.Description.Trim();
            product.Category = request.Category.Trim();
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.ReorderLevel = request.ReorderLevel;
            product.IsActive = request.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            activityService.Record("product.updated", product.Name, $"Product {product.Sku} updated.", updatedBy);
            return CloneProduct(product);
        }
    }

    public Product? AdjustStock(string id, int delta, string updatedBy)
    {
        lock (state.SyncRoot)
        {
            var product = state.Products.FirstOrDefault(item => item.Id == id);
            if (product == null)
            {
                return null;
            }

            product.StockQuantity = Math.Max(0, product.StockQuantity + delta);
            product.UpdatedAtUtc = DateTime.UtcNow;

            activityService.Record("product.stockAdjusted", product.Name,
                $"Stock adjusted by {delta} units.", updatedBy);
            return CloneProduct(product);
        }
    }

    public Product? AttachImage(string id, FilePart file, string updatedBy)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var fileName = $"{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var fullPath = Path.Combine(uploadRoot, fileName);
        file.SaveToDisk(fullPath);

        lock (state.SyncRoot)
        {
            var product = state.Products.FirstOrDefault(item => item.Id == id);
            if (product == null)
            {
                return null;
            }

            product.ImageUrl = $"/uploads/products/{fileName}";
            product.UpdatedAtUtc = DateTime.UtcNow;

            activityService.Record("product.imageUploaded", product.Name, $"Image uploaded for {product.Sku}.", updatedBy);
            return CloneProduct(product);
        }
    }

    internal Product? GetInternal(string id)
    {
        lock (state.SyncRoot)
        {
            return state.Products.FirstOrDefault(item => item.Id == id);
        }
    }

    private static Product CloneProduct(Product product)
    {
        return new Product
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            Category = product.Category,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            ReorderLevel = product.ReorderLevel,
            IsActive = product.IsActive,
            ImageUrl = product.ImageUrl,
            CreatedAtUtc = product.CreatedAtUtc,
            UpdatedAtUtc = product.UpdatedAtUtc
        };
    }
}

public sealed class CustomerService(BackofficeState state, ActivityService activityService)
{
    public PagedResult<Customer> List(string search, int page, int pageSize)
    {
        lock (state.SyncRoot)
        {
            IEnumerable<Customer> query = state.Customers;
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item =>
                    item.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.Code.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var totalCount = query.Count();
            var items = query
                .OrderBy(item => item.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneCustomer)
                .ToList();

            return new PagedResult<Customer>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }

    public Customer? Get(string id)
    {
        lock (state.SyncRoot)
        {
            return state.Customers.Where(item => item.Id == id).Select(CloneCustomer).FirstOrDefault();
        }
    }

    public Customer Create(CreateCustomerRequest request, string createdBy)
    {
        var customer = new Customer
        {
            Id = IdGenerator.EntityId("cus"),
            Code = $"C-{Convert.ToHexString(RandomNumberGenerator.GetBytes(2))}",
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            VatNumber = request.VatNumber.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            Notes = request.Notes.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        lock (state.SyncRoot)
        {
            state.Customers.Add(customer);
        }

        activityService.Record("customer.created", customer.Name, $"Customer {customer.Code} created.", createdBy);
        return CloneCustomer(customer);
    }

    public Customer? Update(string id, UpdateCustomerRequest request, string updatedBy)
    {
        lock (state.SyncRoot)
        {
            var customer = state.Customers.FirstOrDefault(item => item.Id == id);
            if (customer == null)
            {
                return null;
            }

            customer.Name = request.Name.Trim();
            customer.Email = request.Email.Trim();
            customer.Phone = request.Phone.Trim();
            customer.VatNumber = request.VatNumber.Trim();
            customer.City = request.City.Trim();
            customer.Country = request.Country.Trim();
            customer.Notes = request.Notes.Trim();

            activityService.Record("customer.updated", customer.Name, $"Customer {customer.Code} updated.", updatedBy);
            return CloneCustomer(customer);
        }
    }

    internal Customer? GetInternal(string id)
    {
        lock (state.SyncRoot)
        {
            return state.Customers.FirstOrDefault(item => item.Id == id);
        }
    }

    private static Customer CloneCustomer(Customer customer)
    {
        return new Customer
        {
            Id = customer.Id,
            Code = customer.Code,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            VatNumber = customer.VatNumber,
            City = customer.City,
            Country = customer.Country,
            Notes = customer.Notes,
            CreatedAtUtc = customer.CreatedAtUtc
        };
    }
}

public sealed class OrderService(BackofficeState state, ActivityService activityService, string uploadRoot)
{
    public PagedResult<OrderRecord> List(string search, string status, int page, int pageSize)
    {
        lock (state.SyncRoot)
        {
            IEnumerable<OrderRecord> query = state.Orders;
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item =>
                    item.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    item.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(item => item.Status == parsedStatus);
            }

            var totalCount = query.Count();
            var items = query
                .OrderByDescending(item => item.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(CloneOrder)
                .ToList();

            return new PagedResult<OrderRecord>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }

    public OrderRecord? Get(string id)
    {
        lock (state.SyncRoot)
        {
            return state.Orders.Where(item => item.Id == id).Select(CloneOrder).FirstOrDefault();
        }
    }

    public List<OrderRecord> ByCustomer(string customerId)
    {
        lock (state.SyncRoot)
        {
            return state.Orders.Where(item => item.CustomerId == customerId)
                .OrderByDescending(item => item.CreatedAtUtc)
                .Select(CloneOrder)
                .ToList();
        }
    }

    public (OrderRecord? order, string? error) Create(CreateOrderRequest request, string createdBy)
    {
        lock (state.SyncRoot)
        {
            var customer = state.Customers.FirstOrDefault(item => item.Id == request.CustomerId);
            if (customer == null)
            {
                return (null, "Customer not found");
            }

            if (request.Items.Count == 0)
            {
                return (null, "At least one order item is required");
            }

            var orderItems = new List<OrderItem>();
            foreach (var item in request.Items)
            {
                var product = state.Products.FirstOrDefault(prod => prod.Id == item.ProductId);
                if (product == null)
                {
                    return (null, $"Product {item.ProductId} not found");
                }

                if (item.Quantity <= 0)
                {
                    return (null, "Quantity must be greater than zero");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return (null, $"Product {product.Name} does not have enough stock");
                }

                product.StockQuantity -= item.Quantity;
                product.UpdatedAtUtc = DateTime.UtcNow;

                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price,
                    LineTotal = product.Price * item.Quantity
                });
            }

            var subtotal = orderItems.Sum(item => item.LineTotal);
            var discount = Math.Max(0, request.Discount);
            var order = new OrderRecord
            {
                Id = IdGenerator.EntityId("ord"),
                OrderNumber = IdGenerator.OrderNumber(),
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Status = OrderStatus.Confirmed,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedBy = createdBy,
                Items = orderItems,
                Subtotal = subtotal,
                Discount = discount,
                Total = Math.Max(0, subtotal - discount),
                Notes = request.Notes.Trim()
            };

            state.Orders.Add(order);
            activityService.Record("order.created", order.OrderNumber, $"Order created for {order.CustomerName}.", createdBy);
            return (CloneOrder(order), null);
        }
    }

    public OrderRecord? UpdateStatus(string id, OrderStatus status, string updatedBy)
    {
        lock (state.SyncRoot)
        {
            var order = state.Orders.FirstOrDefault(item => item.Id == id);
            if (order == null)
            {
                return null;
            }

            order.Status = status;
            activityService.Record("order.statusChanged", order.OrderNumber,
                $"Order status changed to {status}.", updatedBy);
            return CloneOrder(order);
        }
    }

    public OrderRecord? AttachFile(string id, FilePart file, string updatedBy)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var fileName = $"{id}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var fullPath = Path.Combine(uploadRoot, fileName);
        file.SaveToDisk(fullPath);

        lock (state.SyncRoot)
        {
            var order = state.Orders.FirstOrDefault(item => item.Id == id);
            if (order == null)
            {
                return null;
            }

            order.AttachmentFileName = file.FileName;
            order.AttachmentUrl = $"/uploads/orders/{fileName}";

            activityService.Record("order.attachmentUploaded", order.OrderNumber,
                $"Attachment uploaded for order {order.OrderNumber}.", updatedBy);
            return CloneOrder(order);
        }
    }

    public List<OrderRecord> ExportSnapshot()
    {
        lock (state.SyncRoot)
        {
            return state.Orders.OrderByDescending(item => item.CreatedAtUtc).Select(CloneOrder).ToList();
        }
    }

    private static OrderRecord CloneOrder(OrderRecord order)
    {
        return new OrderRecord
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            Status = order.Status,
            CreatedAtUtc = order.CreatedAtUtc,
            CreatedBy = order.CreatedBy,
            Items = order.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.LineTotal
            }).ToList(),
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            Total = order.Total,
            Notes = order.Notes,
            AttachmentFileName = order.AttachmentFileName,
            AttachmentUrl = order.AttachmentUrl
        };
    }
}

public sealed class InventoryService(BackofficeState state, ActivityService activityService)
{
    public List<InventoryAdjustment> List(int take = 50)
    {
        lock (state.SyncRoot)
        {
            return state.InventoryAdjustments
                .OrderByDescending(item => item.CreatedAtUtc)
                .Take(take)
                .Select(CloneAdjustment)
                .ToList();
        }
    }

    public List<Product> LowStock()
    {
        lock (state.SyncRoot)
        {
            return state.Products
                .Where(item => item.StockQuantity <= item.ReorderLevel)
                .OrderBy(item => item.StockQuantity)
                .Select(item => new Product
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    Description = item.Description,
                    Category = item.Category,
                    Price = item.Price,
                    StockQuantity = item.StockQuantity,
                    ReorderLevel = item.ReorderLevel,
                    IsActive = item.IsActive,
                    ImageUrl = item.ImageUrl,
                    CreatedAtUtc = item.CreatedAtUtc,
                    UpdatedAtUtc = item.UpdatedAtUtc
                })
                .ToList();
        }
    }

    public (InventoryAdjustment? adjustment, string? error) Create(CreateInventoryAdjustmentRequest request, string createdBy)
    {
        lock (state.SyncRoot)
        {
            var product = state.Products.FirstOrDefault(item => item.Id == request.ProductId);
            if (product == null)
            {
                return (null, "Product not found");
            }

            var nextStock = product.StockQuantity + request.QuantityDelta;
            if (nextStock < 0)
            {
                return (null, "Inventory cannot become negative");
            }

            product.StockQuantity = nextStock;
            product.UpdatedAtUtc = DateTime.UtcNow;

            var adjustment = new InventoryAdjustment
            {
                Id = IdGenerator.EntityId("adj"),
                ProductId = product.Id,
                ProductName = product.Name,
                Type = request.Type,
                QuantityDelta = request.QuantityDelta,
                Reason = request.Reason.Trim(),
                CreatedBy = createdBy,
                CreatedAtUtc = DateTime.UtcNow
            };

            state.InventoryAdjustments.Insert(0, adjustment);
            activityService.Record("inventory.adjusted", product.Name,
                $"Inventory adjusted by {request.QuantityDelta} units ({request.Type}).", createdBy);
            return (CloneAdjustment(adjustment), null);
        }
    }

    private static InventoryAdjustment CloneAdjustment(InventoryAdjustment adjustment)
    {
        return new InventoryAdjustment
        {
            Id = adjustment.Id,
            ProductId = adjustment.ProductId,
            ProductName = adjustment.ProductName,
            Type = adjustment.Type,
            QuantityDelta = adjustment.QuantityDelta,
            Reason = adjustment.Reason,
            CreatedBy = adjustment.CreatedBy,
            CreatedAtUtc = adjustment.CreatedAtUtc
        };
    }
}

public sealed class DashboardService(BackofficeState state)
{
    public DashboardSummaryResponse GetSummary()
    {
        lock (state.SyncRoot)
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            return new DashboardSummaryResponse
            {
                TotalProducts = state.Products.Count,
                TotalCustomers = state.Customers.Count,
                TotalOrders = state.Orders.Count,
                OrdersToday = state.Orders.Count(item => item.CreatedAtUtc.Date == today),
                LowStockItems = state.Products.Count(item => item.StockQuantity <= item.ReorderLevel),
                RevenueMonth = state.Orders
                    .Where(item => item.CreatedAtUtc >= monthStart && item.Status != OrderStatus.Cancelled)
                    .Sum(item => item.Total)
            };
        }
    }

    public List<MetricPoint> GetSalesSeries(int days = 7)
    {
        lock (state.SyncRoot)
        {
            var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
            return Enumerable.Range(0, days)
                .Select(offset =>
                {
                    var day = start.AddDays(offset);
                    return new MetricPoint
                    {
                        Label = day.ToString("MM-dd"),
                        Value = state.Orders
                            .Where(item => item.CreatedAtUtc.Date == day && item.Status != OrderStatus.Cancelled)
                            .Sum(item => item.Total)
                    };
                })
                .ToList();
        }
    }

    public List<Product> GetLowStock()
    {
        lock (state.SyncRoot)
        {
            return state.Products
                .Where(item => item.StockQuantity <= item.ReorderLevel)
                .OrderBy(item => item.StockQuantity)
                .Take(6)
                .Select(item => new Product
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    Category = item.Category,
                    StockQuantity = item.StockQuantity,
                    ReorderLevel = item.ReorderLevel,
                    Price = item.Price,
                    CreatedAtUtc = item.CreatedAtUtc,
                    UpdatedAtUtc = item.UpdatedAtUtc
                })
                .ToList();
        }
    }

    public List<OrderRecord> GetRecentOrders(int take = 6)
    {
        lock (state.SyncRoot)
        {
            return state.Orders.OrderByDescending(item => item.CreatedAtUtc).Take(take)
                .Select(item => new OrderRecord
                {
                    Id = item.Id,
                    OrderNumber = item.OrderNumber,
                    CustomerId = item.CustomerId,
                    CustomerName = item.CustomerName,
                    Status = item.Status,
                    CreatedAtUtc = item.CreatedAtUtc,
                    CreatedBy = item.CreatedBy,
                    Subtotal = item.Subtotal,
                    Discount = item.Discount,
                    Total = item.Total,
                    Notes = item.Notes
                })
                .ToList();
        }
    }
}
