namespace Application.DTOs;

public class OrderDto
{
    public Guid OrderId { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ShopLogoUrl { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingMethod { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string PayoutStatus { get; set; } = "Pending";
    public string? SlipImageUrl { get; set; }
    public DateTime? SlipUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ShippingAddress { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public ShipmentDto? Shipment { get; set; }
}

public sealed class ProductStockDto
{
    public Guid StockId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdateOrderStatusDto
{
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public string? SlipImageUrl { get; set; }
    public DateTime? SlipUploadedAt { get; set; }
}

public sealed class OrderDetailDto : OrderDto
{
}

public sealed class OrderItemDto
{
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public sealed class ShipmentDto
{
    public string? ShippingProvider { get; set; }
    public string? TrackingNumber { get; set; }
    public string ShippingStatus { get; set; } = "Pending";
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
}

public sealed class UpdateShipmentDto
{
    public string ShippingProvider { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
}

public sealed class UpdateShipmentStatusDto
{
    public string Status { get; set; } = string.Empty;
}

public sealed class VerifySlipDto
{
    public List<Guid> OrderIds { get; set; } = [];
    public string Action { get; set; } = "Approve"; // "Approve" or "Reject"
    public string? Note { get; set; }
}

public sealed class AdminPendingSlipGroupDto
{
    public string SlipImageUrl { get; set; } = string.Empty;
    public DateTime? SlipUploadedAt { get; set; }
    public decimal TotalGroupAmount { get; set; }
    public string? BuyerName { get; set; }
    public string? BuyerPhone { get; set; }
    public List<OrderDto> Orders { get; set; } = [];
}

public sealed class AdminMerchantPayoutGroupDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLogoUrl { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? PromptPay { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public int PaidOrdersCount { get; set; }
    public List<OrderDto> Orders { get; set; } = [];
}

public sealed class ConfirmPayoutRequestDto
{
    public Guid ShopId { get; set; }
    public List<Guid> OrderIds { get; set; } = [];
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
}

public sealed class MerchantPayoutRecordDto
{
    public Guid PayoutId { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ShopLogoUrl { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SlipImageUrl { get; set; }
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; }
    public int OrdersCount { get; set; }
    public List<OrderDto> Orders { get; set; } = [];
}
