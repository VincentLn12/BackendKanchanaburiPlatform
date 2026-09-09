using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateTagDto
{
    [Required, StringLength(100)] public string TagName { get; set; } = string.Empty;
}

public sealed class UpdateTagDto : CreateTagDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class TagDto
{
    public Guid TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
