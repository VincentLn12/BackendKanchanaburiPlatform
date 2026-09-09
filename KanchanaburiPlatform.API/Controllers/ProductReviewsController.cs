using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KanchanaburiPlatform.Domain.Entities;
using Core.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/product-reviews")]
public sealed class ProductReviewsController(IUnitOfWork unit, UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductReviewsDto>> GetReviews(Guid productId)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(productId);
        if (product?.Status != "Active") return NotFound();

        var reviews = (await unit.Repository<Review>().ListAllAsync())
            .Where(review => review.ProductId == productId && review.Status == "Published")
            .OrderByDescending(review => review.CreatedAt)
            .ToList();

        var ratingCounts = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };
        foreach (var r in reviews)
        {
            if (ratingCounts.ContainsKey(r.Rating))
                ratingCounts[r.Rating]++;
        }

        var items = await Task.WhenAll(reviews.Select(ToDto));
        return Ok(new ProductReviewsDto
        {
            TotalCount = reviews.Count,
            AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(review => review.Rating), 1),
            RatingCounts = ratingCounts,
            Reviews = items.ToList(),
        });
    }

    [HttpGet("my-reviews")]
    [Authorize]
    public async Task<ActionResult<List<Guid>>> GetMyReviewedProductIds()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var reviewedProductIds = (await unit.Repository<Review>().ListAllAsync())
            .Where(r => r.UserId == userId && r.ProductId != null)
            .Select(r => r.ProductId!.Value)
            .Distinct()
            .ToList();

        return Ok(reviewedProductIds);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ProductReviewDto>> CreateReview([FromBody] CreateProductReviewDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5) return BadRequest("คะแนนต้องอยู่ระหว่าง 1 ถึง 5 ดาว");
        if (string.IsNullOrWhiteSpace(dto.Comment)) return BadRequest("กรุณากรอกข้อความรีวิว");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var product = await unit.Repository<Product>().GetByIdAsync(dto.ProductId);
        if (product == null) return NotFound("ไม่พบสินค้า");

        // Verify purchase in completed or shipped order
        var orders = (await unit.Repository<Order>().ListAllAsync())
            .Where(o => o.UserId == userId && (o.OrderStatus == "Completed" || o.OrderStatus == "Shipped"))
            .ToList();

        var orderItems = (await unit.Repository<OrderItem>().ListAllAsync())
            .Where(oi => oi.ProductId == dto.ProductId && orders.Any(o => o.OrderId == oi.OrderId))
            .ToList();

        if (!orderItems.Any())
        {
            return BadRequest("คุณสามารถเขียนรีวิวได้เฉพาะสินค้าที่คุณเคยสั่งซื้อและได้รับการจัดส่งแล้วเท่านั้น");
        }

        var existingReview = (await unit.Repository<Review>().ListAllAsync())
            .FirstOrDefault(r => r.UserId == userId && r.ProductId == dto.ProductId);

        if (existingReview != null)
        {
            existingReview.Rating = dto.Rating;
            existingReview.Comment = dto.Comment.Trim();
            existingReview.CreatedAt = DateTime.UtcNow;
            existingReview.Status = "Published";
            unit.Repository<Review>().Update(existingReview);
            await unit.Complete();
            return Ok(await ToDto(existingReview));
        }

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            UserId = userId,
            ProductId = dto.ProductId,
            ShopId = product.ShopId,
            Rating = dto.Rating,
            Comment = dto.Comment.Trim(),
            Status = "Published",
            CreatedAt = DateTime.UtcNow
        };

        unit.Repository<Review>().Add(review);
        await unit.Complete();

        return Ok(await ToDto(review));
    }

    private async Task<ProductReviewDto> ToDto(Review review)
    {
        var user = await userManager.FindByIdAsync(review.UserId);
        var userName = $"{user?.FirstName} {user?.LastName}".Trim();
        return new ProductReviewDto
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

public sealed class CreateProductReviewDto
{
    public Guid ProductId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class ProductReviewDto
{
    public Guid ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ProductReviewsDto
{
    public int TotalCount { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingCounts { get; set; } = [];
    public List<ProductReviewDto> Reviews { get; set; } = [];
}

