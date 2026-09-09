namespace API.Controllers;

[ApiController]
[Route("api/shop-reviews")]
public sealed class ShopReviewsController(IUnitOfWork unit, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("{shopId:guid}")]
    public async Task<ActionResult<ShopReviewsDto>> GetReviews(Guid shopId)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId);
        if (shop is null || shop.Status != "Active") return NotFound();

        var reviews = (await unit.Repository<Review>().ListAllAsync())
            .Where(review => review.ShopId == shopId && review.Status == "Published")
            .OrderByDescending(review => review.CreatedAt)
            .ToList();

        var items = await Task.WhenAll(reviews.Take(5).Select(ToDto));
        return Ok(new ShopReviewsDto
        {
            TotalCount = reviews.Count,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(review => review.Rating), 1),
            Reviews = items.ToList(),
        });
    }

    private async Task<ShopReviewDto> ToDto(Review review)
    {
        var user = await userManager.FindByIdAsync(review.UserId);
        var userName = $"{user?.FirstName} {user?.LastName}".Trim();
        return new ShopReviewDto
        {
            ReviewId = review.ReviewId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "ผู้ใช้งาน" : userName,
            Rating = review.Rating,
            Comment = review.Comment,
            Reply = review.Reply,
            CreatedAt = review.CreatedAt,
        };
    }
}

public sealed class ShopReviewDto
{
    public Guid ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ShopReviewsDto
{
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public List<ShopReviewDto> Reviews { get; set; } = [];
}
