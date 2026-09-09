namespace KanchanaburiPlatform.Domain.Entities;

public sealed class DataSource
{
    public Guid DataSourceId { get; set; }

    public Guid ContentId { get; set; }

    public string SourceName { get; set; } = string.Empty;
    public string? SourceType { get; set; }

    public string? SourceUrl { get; set; }

    public string? ReferenceDetail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Content Content { get; set; } = null!;
}
