using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class SaveContentDto
{
    [Required, StringLength(300)] public string Title { get; set; } = string.Empty;
    [StringLength(500)] public string? Summary { get; set; }
    public Guid ContentCategoryId { get; set; }
    public Guid? ShopId { get; set; }
    public Guid? DistrictId { get; set; }
    public Guid? SubDistrictId { get; set; }
    [Range(-90, 90)] public decimal? Latitude { get; set; }
    [Range(-180, 180)] public decimal? Longitude { get; set; }
    [Url, StringLength(500)] public string? YoutubeUrl { get; set; }
    [StringLength(50)] public string? Status { get; set; }
}

public sealed class CreateContentDto : SaveContentDto { }

public sealed class UpdateContentDto : SaveContentDto
{
    public new string Status { get; set; } = "Published";
}

public sealed class ContentDto
{
    public Guid ContentId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public Guid ContentCategoryId { get; set; }
    public string? ContentCategoryName { get; set; }
    public Guid? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public Guid? SubDistrictId { get; set; }
    public string? SubDistrictName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? YoutubeUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public List<TagDto> Tags { get; set; } = [];
}
