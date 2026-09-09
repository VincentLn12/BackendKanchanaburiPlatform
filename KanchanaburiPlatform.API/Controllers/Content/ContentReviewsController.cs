using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/content-reviews")]
public sealed class ContentReviewsController(IUnitOfWork unit, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("{contentId:guid}")]
    public async Task<ActionResult<ContentReviewsDto>> GetReviews(Guid contentId)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(contentId);
        if (content is null || content.Status != "Published") return NotFound();
        var reviews = (await unit.Repository<Review>().ListAllAsync())
            .Where(review => review.ContentId == contentId && review.Status == "Published")
            .OrderByDescending(review => review.CreatedAt)
            .ToList();
        return Ok(await ToListDto(reviews));
    }

    [HttpGet("mine/{contentId:guid}"), Authorize]
    public async Task<ActionResult<ReviewDto?>> GetMyReview(Guid contentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var review = (await unit.Repository<Review>().ListAllAsync())
            .FirstOrDefault(item => item.ContentId == contentId && item.UserId == userId);
        return Ok(review is null ? null : await ToDto(review));
    }

    [HttpPost("{contentId:guid}"), Authorize]
    public async Task<ActionResult<ReviewDto>> CreateOrReplaceReview(Guid contentId, SaveReviewDto dto)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(contentId);
        if (content is null || content.Status != "Published") return NotFound();
        if (!IsValid(dto)) return BadRequest("A comment is required.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var existing = (await unit.Repository<Review>().ListAllAsync())
            .FirstOrDefault(item => item.ContentId == contentId && item.UserId == userId);
        if (existing is not null)
        {
            existing.Comment = dto.Comment.Trim();
            existing.Status = "Published";
            unit.Repository<Review>().Update(existing);
            return await unit.Complete() ? Ok(await ToDto(existing)) : BadRequest("Problem saving review.");
        }

        var review = new Review { ReviewId = Guid.NewGuid(), UserId = userId, ContentId = contentId, Rating = 0, Comment = dto.Comment.Trim(), Status = "Published", CreatedAt = DateTime.UtcNow };
        unit.Repository<Review>().Add(review);
        return await unit.Complete() ? CreatedAtAction(nameof(GetMyReview), new { contentId }, await ToDto(review)) : BadRequest("Problem saving review.");
    }

    [HttpPut("{reviewId:guid}"), Authorize]
    public async Task<ActionResult<ReviewDto>> UpdateReview(Guid reviewId, SaveReviewDto dto)
    {
        if (!IsValid(dto)) return BadRequest("A comment is required.");
        var review = await unit.Repository<Review>().GetByIdAsync(reviewId);
        if (review is null) return NotFound();
        if (!User.IsInRole("Admin") && review.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        review.Comment = dto.Comment.Trim();
        review.Status = "Published";
        unit.Repository<Review>().Update(review);
        return await unit.Complete() ? Ok(await ToDto(review)) : BadRequest("Problem saving review.");
    }

    [HttpDelete("{reviewId:guid}"), Authorize]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        var review = await unit.Repository<Review>().GetByIdAsync(reviewId);
        if (review is null) return NotFound();
        if (!User.IsInRole("Admin") && review.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        unit.Repository<Review>().Remove(review);
        return await unit.Complete() ? NoContent() : BadRequest("Problem deleting review.");
    }

    private static bool IsValid(SaveReviewDto dto) => !string.IsNullOrWhiteSpace(dto.Comment) && dto.Comment.Length <= 2000;

    private async Task<ContentReviewsDto> ToListDto(IReadOnlyList<Review> reviews) => new()
    {
        TotalCount = reviews.Count,
        Reviews = (await Task.WhenAll(reviews.Select(ToDto))).ToList()
    };

    private async Task<ReviewDto> ToDto(Review review)
    {
        var user = await userManager.FindByIdAsync(review.UserId);
        return new ReviewDto { ReviewId = review.ReviewId, Comment = review.Comment, UserName = $"{user?.FirstName} {user?.LastName}".Trim() is { Length: > 0 } name ? name : "ผู้ใช้งาน", CreatedAt = review.CreatedAt };
    }
}

public sealed class SaveReviewDto
{
    public string Comment { get; set; } = string.Empty;
}

public sealed class ReviewDto
{
    public Guid ReviewId { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ContentReviewsDto
{
    public int TotalCount { get; set; }
    public List<ReviewDto> Reviews { get; set; } = [];
}
