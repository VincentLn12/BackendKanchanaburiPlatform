namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Shipment
{
    public Guid ShipmentId { get; set; }

    public Guid OrderId { get; set; }

    public string? ShippingProvider { get; set; }

    public string? TrackingNumber { get; set; }

    public string ShippingStatus { get; set; } = "Pending";
    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public Order Order { get; set; } = null!;
}
