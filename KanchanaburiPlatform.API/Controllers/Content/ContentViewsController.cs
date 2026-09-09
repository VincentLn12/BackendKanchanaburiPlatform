using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers.Content;

[ApiController, Authorize]
[Route("api/content-views")]
public sealed class ContentViewsController(IUnitOfWork unit, StoreContext context) : ControllerBase
{
    [HttpPost("{contentId:guid}"), AllowAnonymous]
    public async Task<IActionResult> RecordView(Guid contentId)
    {
        var content = await context.Contents.AsNoTracking().FirstOrDefaultAsync(x => x.ContentId == contentId);
        if (content is null || content.Status != "Published") return NotFound();

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var trackingId = string.IsNullOrEmpty(userId)
            ? $"guest_{HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anon"}"
            : userId;

        var view = await context.ContentViews
            .FirstOrDefaultAsync(item => item.UserId == trackingId && item.ContentId == contentId);

        if (view is null)
        {
            context.ContentViews.Add(new ContentView { ContentViewId = Guid.NewGuid(), ContentId = contentId, UserId = trackingId, ViewedAt = DateTime.UtcNow });
        }
        else
        {
            view.ViewedAt = DateTime.UtcNow;
            context.ContentViews.Update(view);
        }
        return await context.SaveChangesAsync() > 0 ? NoContent() : BadRequest("Problem recording content view.");
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<ContentViewHistoryDto>>> GetHistory(int take = 24)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        take = Math.Clamp(take, 1, 100);
        var recentViews = (await unit.Repository<ContentView>().ListAllAsync())
            .Where(item => item.UserId == userId)
            .GroupBy(item => item.ContentId)
            .Select(group => group.OrderByDescending(item => item.ViewedAt).First())
            .OrderByDescending(item => item.ViewedAt)
            .Take(take)
            .ToList();
        var contents = await unit.Repository<ContentEntity>().ListAllAsync();
        var contentById = contents.Where(content => content.Status == "Published").ToDictionary(content => content.ContentId);
        return Ok(recentViews
            .Where(view => contentById.ContainsKey(view.ContentId))
            .Select(view => new ContentViewHistoryDto
            {
                ContentId = view.ContentId,
                Title = contentById[view.ContentId].Title,
                Summary = contentById[view.ContentId].Summary,
                YoutubeUrl = contentById[view.ContentId].YoutubeUrl,
                ViewedAt = view.ViewedAt
            }).ToList());
    }
}

public sealed class ContentViewHistoryDto
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? YoutubeUrl { get; set; }
    public DateTime ViewedAt { get; set; }
}
