namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ContentRelation
{
    public Guid ContentRelationId { get; set; }

    public Guid ContentId { get; set; }

    public Guid RelatedContentId { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Content Content { get; set; } = null!;
    public Content RelatedContent { get; set; } = null!;
}
