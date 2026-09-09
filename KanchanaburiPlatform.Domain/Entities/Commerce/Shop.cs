namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Shop
{
    public Guid ShopId { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public Guid ShopCategoryId { get; set; }
    public Guid DistrictId { get; set; }
    public Guid SubDistrictId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Status { get; set; } = "Active";
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? PromptPay { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ShopCategory ShopCategory { get; set; } = null!;
    public District District { get; set; } = null!;
    public SubDistrict SubDistrict { get; set; } = null!;
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
   // public ICollection<Content> Contents { get; set; } = new List<Content>();
}
