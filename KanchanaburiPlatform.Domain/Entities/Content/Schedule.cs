namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Schedule
{
    public Guid ScheduleId { get; set; }

    public Guid ContentId { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }

    public DateTime? EndDateTime { get; set; }

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Content Content { get; set; } = null!;
}
