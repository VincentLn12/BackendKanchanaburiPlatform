namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Cart
{
    public Guid CartId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
