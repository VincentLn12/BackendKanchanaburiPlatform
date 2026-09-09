namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ShopCategory
{
    public Guid ShopCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }

    public string Status { get; set; } = "Active";
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
