namespace API.DTOs;

public sealed record CreatePaymentIntentResponse(string ClientSecret, string PaymentIntentId);

public sealed class BatchPaymentRequestDto
{
    public List<Guid> OrderIds { get; set; } = [];
}

public sealed class OrderSummaryItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}

public sealed class BatchOrderSummaryDto
{
    public List<OrderSummaryItemDto> Orders { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public bool IsAllPaid { get; set; }
}
