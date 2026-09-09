namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ContentFavorite
{
    public Guid ContentFavoriteId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid ContentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Content Content { get; set; } = null!;
}
