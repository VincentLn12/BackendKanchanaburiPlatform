namespace KanchanaburiPlatform.Domain.Entities;

public sealed class ContentView
{
    public Guid ContentViewId { get; set; }

    public Guid ContentId { get; set; }

    public string? UserId { get; set; }

    public string? SessionId { get; set; }

    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;
    public Content Content { get; set; } = null!;
}
