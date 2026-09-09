using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateContentCategoryDto
{
    [Required, StringLength(150)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(1000)] public string? Description { get; set; }
}

public sealed class UpdateContentCategoryDto : CreateContentCategoryDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class ContentCategoryDto
{
    public Guid ContentCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
}
