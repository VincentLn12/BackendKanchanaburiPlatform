namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ProductCategory
{
    public Guid ProductCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string Status { get; set; } = "Active";
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
