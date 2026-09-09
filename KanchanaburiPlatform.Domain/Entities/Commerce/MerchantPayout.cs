namespace KanchanaburiPlatform.Domain.Entities;

public sealed class MerchantPayout
{
    public Guid PayoutId { get; set; }
    public Guid ShopId { get; set; }
    public decimal TotalAmount { get; set; }
    public string? SlipImageUrl { get; set; }
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Shop Shop { get; set; } = null!;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
