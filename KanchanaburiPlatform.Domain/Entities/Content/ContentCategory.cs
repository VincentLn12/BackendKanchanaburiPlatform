namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ContentCategory
{
    public Guid ContentCategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string Status { get; set; } = "Active";
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
