namespace BackofficeDemo.Backend.Models;

public enum UserRole
{
    Admin,
    Manager,
    Operator
}

public enum OrderStatus
{
    Draft,
    Confirmed,
    Packed,
    Shipped,
    Completed,
    Cancelled
}

public enum InventoryAdjustmentType
{
    Restock,
    Correction,
    Damage,
    ManualDecrease
}

public sealed class UserAccount
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public DateTime? LastLoginAtUtc { get; set; }
}

public sealed class Product
{
    public string Id { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public sealed class OrderRecord
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = [];
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string AttachmentFileName { get; set; } = string.Empty;
    public string AttachmentUrl { get; set; } = string.Empty;
}

public sealed class InventoryAdjustment
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public InventoryAdjustmentType Type { get; set; }
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AuditEvent
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class NotificationEnvelope
{
    public string Type { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public object Payload { get; set; } = new { };
}

public sealed class BackofficeState
{
    public object SyncRoot { get; } = new();
    public List<UserAccount> Users { get; } = [];
    public List<Product> Products { get; } = [];
    public List<Customer> Customers { get; } = [];
    public List<OrderRecord> Orders { get; } = [];
    public List<InventoryAdjustment> InventoryAdjustments { get; } = [];
    public List<AuditEvent> AuditEvents { get; } = [];
}
