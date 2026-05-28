using BackofficeDemo.Backend.Models;

namespace BackofficeDemo.Backend.Contracts.Requests;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CreateProductRequest
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateProductRequest : CreateProductRequest
{
}

public sealed class UpdateStockRequest
{
    public int QuantityDelta { get; set; }
}

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public sealed class UpdateCustomerRequest : CreateCustomerRequest
{
}

public sealed class CreateOrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}

public sealed class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public sealed class CreateInventoryAdjustmentRequest
{
    public string ProductId { get; set; } = string.Empty;
    public InventoryAdjustmentType Type { get; set; }
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
}
