namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Product
{
    public Guid ProductId { get; set; }

    public Guid ShopId { get; set; }

    public Guid ProductCategoryId { get; set; }

    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public string? ImageUrl { get; set; }
    // เก็บ URL ของรูปประกอบเป็น JSON array ในตาราง Products เดียว
    public string DetailImageUrls { get; set; } = "[]";

    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Shop Shop { get; set; } = null!;
    public ProductCategory ProductCategory { get; set; } = null!;
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
