using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IUnitOfWork unit) : ControllerBase
{
    [HttpPost, Authorize]
    public async Task<ActionResult<ReportDto>> CreateReport(CreateReportDto dto)
    {
        if ((dto.ContentId.HasValue ? 1 : 0) + (dto.ReviewId.HasValue ? 1 : 0) != 1)
            return BadRequest("Select one item to report.");
        if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length > 300 || dto.Description?.Length > 2000)
            return BadRequest("Invalid report details.");
        if (dto.ContentId is { } contentId && await unit.Repository<ContentEntity>().GetByIdAsync(contentId) is not { Status: "Published" }) return NotFound();
        if (dto.ReviewId is { } reviewId && await unit.Repository<Review>().GetByIdAsync(reviewId) is not { Status: "Published" }) return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var exists = (await unit.Repository<Report>().ListAllAsync()).Any(report => report.UserId == userId && report.ContentId == dto.ContentId && report.ReviewId == dto.ReviewId && report.Status is "Pending" or "InProgress");
        if (exists) return Conflict("You have already reported this item.");
        var report = new Report { ReportId = Guid.NewGuid(), UserId = userId, ContentId = dto.ContentId, ReviewId = dto.ReviewId, Reason = dto.Reason.Trim(), Description = dto.Description?.Trim(), Status = "Pending", CreatedAt = DateTime.UtcNow };
        unit.Repository<Report>().Add(report);
        return await unit.Complete() ? Ok(await ToDto(report)) : BadRequest("Problem creating report.");
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ReportDto>>> GetReports(string? status)
    {
        var reports = (await unit.Repository<Report>().ListAllAsync()).Where(report => string.IsNullOrWhiteSpace(status) || report.Status == status).OrderByDescending(report => report.CreatedAt).ToList();
        return Ok(await Task.WhenAll(reports.Select(ToDto)));
    }

    [HttpPatch("{reportId:guid}/status"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ReportDto>> UpdateStatus(Guid reportId, UpdateReportStatusDto dto)
    {
        if (dto.Status is not ("Pending" or "InProgress" or "Resolved" or "Dismissed")) return BadRequest("Invalid report status.");
        var report = await unit.Repository<Report>().GetByIdAsync(reportId);
        if (report is null) return NotFound();
        report.Status = dto.Status;
        unit.Repository<Report>().Update(report);
        return await unit.Complete() ? Ok(await ToDto(report)) : BadRequest("Problem updating report.");
    }

    private async Task<ReportDto> ToDto(Report report)
    {
        var contentTitle = report.ContentId.HasValue ? (await unit.Repository<ContentEntity>().GetByIdAsync(report.ContentId.Value))?.Title : null;
        var review = report.ReviewId.HasValue ? await unit.Repository<Review>().GetByIdAsync(report.ReviewId.Value) : null;
        return new ReportDto { ReportId = report.ReportId, ContentId = report.ContentId, ReviewId = report.ReviewId, Reason = report.Reason, Description = report.Description, Status = report.Status, CreatedAt = report.CreatedAt, TargetLabel = contentTitle ?? (review is null ? "รายการที่ถูกรายงาน" : $"รีวิว: {review.Comment[..Math.Min(review.Comment.Length, 80)]}") };
    }
}

public sealed class CreateReportDto { public Guid? ContentId { get; set; } public Guid? ReviewId { get; set; } public string Reason { get; set; } = string.Empty; public string? Description { get; set; } }
public sealed class UpdateReportStatusDto { public string Status { get; set; } = "Pending"; }
public sealed class ReportDto
{
    public Guid ReportId { get; set; }
    public Guid? ContentId { get; set; }
    public Guid? ReviewId { get; set; }
    public string TargetLabel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
