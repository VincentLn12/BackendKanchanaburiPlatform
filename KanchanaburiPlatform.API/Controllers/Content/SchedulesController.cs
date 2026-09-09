using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/schedules")]
public sealed class SchedulesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetSchedules(
        Guid? contentId, DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && to < from) return BadRequest("The end date must not be before the start date.");

        var contents = (await unit.Repository<ContentEntity>().ListAllAsync())
            .Where(content => content.Status == "Published")
            .ToDictionary(content => content.ContentId);
        var schedules = (await unit.Repository<Schedule>().ListAllAsync())
            .Where(schedule => schedule.Status == "Active" && contents.ContainsKey(schedule.ContentId))
            .Where(schedule => !contentId.HasValue || schedule.ContentId == contentId.Value)
            .Where(schedule => !from.HasValue || schedule.StartDateTime >= from.Value)
            .Where(schedule => !to.HasValue || schedule.StartDateTime <= to.Value)
            .OrderBy(schedule => schedule.StartDateTime)
            .Select(schedule => ToDto(schedule, contents[schedule.ContentId].Title))
            .ToList();
        return Ok(schedules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetByIdAsync(id);
        if (schedule is null || schedule.Status != "Active") return NotFound();
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(schedule.ContentId);
        return content is null || content.Status != "Published" ? NotFound() : Ok(ToDto(schedule, content.Title));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<ScheduleDto>>> GetSchedulesForAdmin(Guid? contentId)
    {
        var contents = (await unit.Repository<ContentEntity>().ListAllAsync()).ToDictionary(content => content.ContentId);
        var schedules = (await unit.Repository<Schedule>().ListAllAsync())
            .Where(schedule => !contentId.HasValue || schedule.ContentId == contentId.Value)
            .Where(schedule => contents.ContainsKey(schedule.ContentId))
            .OrderBy(schedule => schedule.StartDateTime)
            .Select(schedule => ToDto(schedule, contents[schedule.ContentId].Title))
            .ToList();
        return Ok(schedules);
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ScheduleDto>> GetScheduleForAdmin(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetByIdAsync(id);
        if (schedule is null) return NotFound();
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(schedule.ContentId);
        return content is null ? NotFound() : Ok(ToDto(schedule, content.Title));
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ScheduleDto>> CreateSchedule(CreateScheduleDto dto)
    {
        var error = await Validate(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanManageScheduleContent(dto.ContentId)) return Forbid();

        var schedule = new Schedule
        {
            ScheduleId = Guid.NewGuid(),
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };
        CopyDto(dto, schedule);
        unit.Repository<Schedule>().Add(schedule);
        if (!await unit.Complete()) return BadRequest("Problem creating schedule.");

        var content = await unit.Repository<ContentEntity>().GetByIdAsync(schedule.ContentId);
        return CreatedAtAction(nameof(GetSchedule), new { id = schedule.ScheduleId }, ToDto(schedule, content!.Title));
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateSchedule(Guid id, UpdateScheduleDto dto)
    {
        var schedule = await unit.Repository<Schedule>().GetByIdAsync(id);
        if (schedule is null) return NotFound();
        if (!await CanManageScheduleContent(schedule.ContentId)) return Forbid();
        var error = await Validate(dto);
        if (error is not null) return BadRequest(error);
        if (dto.Status is not ("Active" or "Inactive" or "Cancelled")) return BadRequest("Invalid schedule status.");

        CopyDto(dto, schedule);
        schedule.Status = dto.Status;
        unit.Repository<Schedule>().Update(schedule);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating schedule.");
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteSchedule(Guid id)
    {
        var schedule = await unit.Repository<Schedule>().GetByIdAsync(id);
        if (schedule is null) return NotFound();
        if (!await CanManageScheduleContent(schedule.ContentId)) return Forbid();
        schedule.Status = "Inactive";
        unit.Repository<Schedule>().Update(schedule);
        return await unit.Complete() ? NoContent() : BadRequest("Problem archiving schedule.");
    }

    private async Task<bool> CanManageScheduleContent(Guid contentId)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(contentId);
        if (content is null) return false;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (User.IsInRole("Admin") || content.CreatedByUserId == currentUserId) return true;
        if (content.ShopId.HasValue)
        {
            var shop = await unit.Repository<Shop>().GetByIdAsync(content.ShopId.Value);
            if (shop is not null && shop.OwnerUserId == currentUserId) return true;
        }
        return false;
    }

    private async Task<string?> Validate(SaveScheduleDto dto)
    {
        if (dto.EndDateTime.HasValue && dto.EndDateTime < dto.StartDateTime)
            return "The end date must not be before the start date.";
        return await unit.Repository<ContentEntity>().GetByIdAsync(dto.ContentId) is null ? "Content not found." : null;
    }

    private static void CopyDto(SaveScheduleDto dto, Schedule schedule)
    {
        schedule.ContentId = dto.ContentId;
        schedule.Title = dto.Title.Trim();
        schedule.StartDateTime = dto.StartDateTime;
        schedule.EndDateTime = dto.EndDateTime;
        schedule.Address = dto.Address?.Trim();
        schedule.Latitude = dto.Latitude;
        schedule.Longitude = dto.Longitude;
        schedule.Description = dto.Description?.Trim();
    }

    private static ScheduleDto ToDto(Schedule schedule, string contentTitle) => new()
    {
        ScheduleId = schedule.ScheduleId,
        ContentId = schedule.ContentId,
        ContentTitle = contentTitle,
        Title = schedule.Title,
        StartDateTime = schedule.StartDateTime,
        EndDateTime = schedule.EndDateTime,
        Address = schedule.Address,
        Latitude = schedule.Latitude,
        Longitude = schedule.Longitude,
        Description = schedule.Description,
        Status = schedule.Status,
        CreatedAt = schedule.CreatedAt
    };
}
