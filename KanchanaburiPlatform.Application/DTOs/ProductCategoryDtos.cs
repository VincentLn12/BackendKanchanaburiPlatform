using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateProductCategoryDto
{
    [Required, StringLength(150)] public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateProductCategoryDto : CreateProductCategoryDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class ProductCategoryDto
{
    public Guid ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}
