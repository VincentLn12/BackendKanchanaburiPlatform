using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateProductDto
{
    public Guid ShopId { get; set; }
    public Guid ProductCategoryId { get; set; }
    [Required, StringLength(250)] public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Range(0, double.MaxValue)] public decimal Price { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class UpdateProductDto : CreateProductDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class UpdateProductStatusDto
{
    [Required] public string Status { get; set; } = string.Empty;
}

public sealed class ProductDto
{
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public Guid ProductCategoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> DetailImages { get; set; } = [];
}
