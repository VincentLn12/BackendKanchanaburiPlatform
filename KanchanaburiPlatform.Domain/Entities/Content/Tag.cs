namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Tag
{
    public Guid TagId { get; set; }

    public string TagName { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public ICollection<ContentTag> ContentTags { get; set; } = new List<ContentTag>();
}
