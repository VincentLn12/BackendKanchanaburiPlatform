using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers.Content;

[ApiController, Authorize]
[Route("api/content-favorites")]
public sealed class ContentFavoritesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FavoriteContentDto>>> GetFavorites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var favorites = (await unit.Repository<ContentFavorite>().ListAllAsync())
            .Where(favorite => favorite.UserId == userId)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .ToList();
        var contents = (await unit.Repository<ContentEntity>().ListAllAsync())
            .Where(content => content.Status == "Published")
            .ToDictionary(content => content.ContentId);
        return Ok(favorites.Where(favorite => contents.ContainsKey(favorite.ContentId)).Select(favorite =>
        {
            var content = contents[favorite.ContentId];
            return new FavoriteContentDto { ContentId = content.ContentId, Title = content.Title, Summary = content.Summary, YoutubeUrl = content.YoutubeUrl, CreatedAt = favorite.CreatedAt };
        }).ToList());
    }

    [HttpGet("{contentId:guid}")]
    public async Task<ActionResult<FavoriteStatusDto>> GetStatus(Guid contentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isFavorite = (await unit.Repository<ContentFavorite>().ListAllAsync())
            .Any(favorite => favorite.UserId == userId && favorite.ContentId == contentId);
        return Ok(new FavoriteStatusDto { IsFavorite = isFavorite });
    }

    [HttpPost("{contentId:guid}")]
    public async Task<ActionResult<FavoriteStatusDto>> AddFavorite(Guid contentId)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(contentId);
        if (content is null || content.Status != "Published") return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var exists = (await unit.Repository<ContentFavorite>().ListAllAsync())
            .Any(favorite => favorite.UserId == userId && favorite.ContentId == contentId);
        if (!exists)
        {
            unit.Repository<ContentFavorite>().Add(new ContentFavorite { ContentFavoriteId = Guid.NewGuid(), UserId = userId, ContentId = contentId, CreatedAt = DateTime.UtcNow });
            if (!await unit.Complete()) return BadRequest("Problem saving favorite.");
        }
        return Ok(new FavoriteStatusDto { IsFavorite = true });
    }

    [HttpDelete("{contentId:guid}")]
    public async Task<ActionResult<FavoriteStatusDto>> RemoveFavorite(Guid contentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var favorite = (await unit.Repository<ContentFavorite>().ListAllAsync())
            .FirstOrDefault(item => item.UserId == userId && item.ContentId == contentId);
        if (favorite is null) return Ok(new FavoriteStatusDto { IsFavorite = false });
        unit.Repository<ContentFavorite>().Remove(favorite);
        return await unit.Complete() ? Ok(new FavoriteStatusDto { IsFavorite = false }) : BadRequest("Problem removing favorite.");
    }
}

public sealed class FavoriteStatusDto { public bool IsFavorite { get; set; } }
public sealed class FavoriteContentDto
{
    public Guid ContentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? YoutubeUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
