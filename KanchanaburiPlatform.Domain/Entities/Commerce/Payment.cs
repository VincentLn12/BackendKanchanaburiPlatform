namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Payment
{
    public Guid PaymentId { get; set; }

    public Guid OrderId { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }

    public decimal Amount { get; set; }

    public string PaymentStatus { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Order Order { get; set; } = null!;
}
