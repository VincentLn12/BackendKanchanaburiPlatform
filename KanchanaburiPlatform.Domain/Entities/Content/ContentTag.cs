namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ContentTag
{
    public Guid ContentId { get; set; }

    public Guid TagId { get; set; }

    public Content Content { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
