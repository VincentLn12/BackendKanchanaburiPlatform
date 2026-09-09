namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Order
{
    public Guid OrderId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public Guid ShopId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal TotalAmount { get; set; }
    public string ShippingMethod { get; set; } = "Delivery";
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ShippingAddress { get; set; }

    public string OrderStatus { get; set; } = "Pending";
    public string PaymentStatus { get; set; } = "Pending";
    public string PayoutStatus { get; set; } = "Pending";
    public Guid? MerchantPayoutId { get; set; }
    public string? SlipImageUrl { get; set; }
    public DateTime? SlipUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Shop Shop { get; set; } = null!;
    public MerchantPayout? MerchantPayout { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
