using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class SaveScheduleDto
{
    [Required] public Guid ContentId { get; set; }
    [Required, StringLength(250)] public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    [StringLength(500)] public string? Address { get; set; }
    [Range(-90, 90)] public decimal? Latitude { get; set; }
    [Range(-180, 180)] public decimal? Longitude { get; set; }
    public string? Description { get; set; }
}

public sealed class CreateScheduleDto : SaveScheduleDto { }

public sealed class UpdateScheduleDto : SaveScheduleDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class ScheduleDto
{
    public Guid ScheduleId { get; set; }
    public Guid ContentId { get; set; }
    public string? ContentTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
