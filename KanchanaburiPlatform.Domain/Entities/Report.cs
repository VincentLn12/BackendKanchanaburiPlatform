namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Report
{
    public Guid ReportId { get; set; }

    public string UserId { get; set; } = string.Empty;
    public Guid? ContentId { get; set; }
    public Guid? ReviewId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Content? Content { get; set; }
    public Review? Review { get; set; }
}
