namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Review
{
    public Guid ReviewId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public Guid? ContentId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ShopId { get; set; }

    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Content? Content { get; set; }
    public Product? Product { get; set; }
    public Shop? Shop { get; set; }
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
