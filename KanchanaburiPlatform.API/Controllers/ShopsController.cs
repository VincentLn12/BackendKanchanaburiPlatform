using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
using KanchanaburiPlatform.Application.Specifications.Commerce;
using Application.DTOs;
namespace API.Controllers;

[ApiController]
[Route("api/shops")]
public sealed class ShopsController(IUnitOfWork unit, ShopBusinessService business, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ShopDto>>> GetShops(
        string? search,
        Guid? categoryId,
        Guid? districtId,
        Guid? subDistrictId,
        string? sortBy = "latest",
        int page = 1,
        int pageSize = 9)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteriaSpec = new ShopSearchSpecification(search, categoryId, districtId, subDistrictId, "Active");
        var pageSpec = new ShopSearchSpecification(search, categoryId, districtId, subDistrictId, "Active", sortBy: sortBy, page: page, pageSize: pageSize);

        var shops = await unit.Repository<Shop>().ListAsync(pageSpec);
        var totalCount = await unit.Repository<Shop>().CountAsync(criteriaSpec);

        return Ok(new PagedResultDto<ShopDto>
        {
            Items = shops.Select(shop => ToDto(shop)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShopDto>> GetShop(Guid id)
    {
        var shop = (await unit.Repository<Shop>().ListAsync(new ShopSearchSpecification(status: "Active", shopId: id))).FirstOrDefault();
        return shop == null ? NotFound() : Ok(ToDto(shop));
    }

    [HttpGet("mine"), Authorize]
    public async Task<ActionResult<ShopDto>> GetMyShop()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var shop = (await unit.Repository<Shop>().ListAsync(new ShopSearchSpecification(ownerUserId: userId))).FirstOrDefault();
        return shop == null ? NotFound() : Ok(ToDto(shop, true));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ShopDto>>> GetAllShopsForAdmin(
        string? search,
        string? status,
        int page = 1,
        int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var criteriaSpec = new ShopSearchSpecification(search: search, status: status);
        var pageSpec = new ShopSearchSpecification(search: search, status: status, page: page, pageSize: pageSize);
        var shops = await unit.Repository<Shop>().ListAsync(pageSpec);
        var totalCount = await unit.Repository<Shop>().CountAsync(criteriaSpec);

        return Ok(new PagedResultDto<ShopDto>
        {
            Items = shops.Select(shop => ToDto(shop, true)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShopDto>> GetShopForAdmin(Guid id)
    {
        var shop = (await unit.Repository<Shop>().ListAsync(new ShopSearchSpecification(shopId: id))).FirstOrDefault();
        return shop == null ? NotFound() : Ok(ToDto(shop, true));
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ShopDto>> CreateShop(CreateShopDto dto)
    {
        var shop = new Shop();
        CopyDtoToShop(dto, shop);
        shop.OwnerUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        try { await business.ValidateCreateAsync(shop); }
        catch (InvalidOperationException ex) { return Conflict(ex.Message); }
        shop.ShopId = Guid.NewGuid(); shop.Status = "PendingApproval"; shop.CreatedAt = shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Add(shop);
        return await unit.Complete() ? CreatedAtAction(nameof(GetShop), new { id = shop.ShopId }, ToDto(shop, true)) : BadRequest("Problem creating shop");
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateShop(Guid id, UpdateShopDto dto)
    {
        var existing = await unit.Repository<Shop>().GetByIdAsync(id);
        if (existing == null) return NotFound();
        var isAdmin = User.IsInRole("Admin");
        try { ShopBusinessService.EnsureOwner(existing, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty, isAdmin); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        if (!isAdmin && (existing.Status == "Closed" || existing.Status == "Suspended"))
            return BadRequest("This shop cannot be updated in its current status");
        CopyDtoToShop(dto, existing); existing.UpdatedAt = DateTime.UtcNow;
        // A rejected applicant can correct the form and submit it for review again.
        if (!isAdmin && existing.Status == "Rejected") existing.Status = "PendingApproval";
        try { await business.ValidateUpdateAsync(existing); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating shop");
    }

    [HttpPost("{id:guid}/cover-image"), Authorize]
    public async Task<ActionResult<ShopDto>> UploadCoverImage(Guid id, IFormFile file)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(id);
        if (shop == null) return NotFound();

        try { ShopBusinessService.EnsureOwner(shop, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, User.IsInRole("Admin")); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var imageUrl = await SaveCoverImageAsync(file);
        if (imageUrl == null) return BadRequest("Only JPG, PNG, WEBP files up to 5 MB are allowed.");

        shop.CoverImageUrl = imageUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);
        return await unit.Complete() ? Ok(ToDto(shop, true)) : BadRequest("Problem uploading shop cover image");
    }

    [HttpPost("{id:guid}/background-image"), Authorize]
    public async Task<ActionResult<ShopDto>> UploadBackgroundImage(Guid id, IFormFile file)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(id);
        if (shop == null) return NotFound();

        try { ShopBusinessService.EnsureOwner(shop, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, User.IsInRole("Admin")); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var imageUrl = await SaveCoverImageAsync(file);
        if (imageUrl == null) return BadRequest("Only JPG, PNG, WEBP files up to 5 MB are allowed.");

        shop.BackgroundImageUrl = imageUrl;
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);
        return await unit.Complete() ? Ok(ToDto(shop, true)) : BadRequest("Problem uploading shop background image");
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteShop(Guid id)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(id);
        if (shop == null) return NotFound();
        try { ShopBusinessService.EnsureOwner(shop, User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty, User.IsInRole("Admin")); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        shop.Status = "Closed"; shop.UpdatedAt = DateTime.UtcNow; unit.Repository<Shop>().Update(shop);
        return await unit.Complete() ? NoContent() : BadRequest("Problem closing shop");
    }

    [HttpPatch("{id:guid}/status"), Authorize]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateShopStatusDto dto)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(id);
        if (shop == null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        try { ShopBusinessService.EnsureOwner(shop, User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, isAdmin); }
        catch (UnauthorizedAccessException) { return Forbid(); }

        var allowed = isAdmin
            ? new[] { "PendingApproval", "Active", "Rejected", "Inactive", "Closed", "Suspended" }
            : new[] { "Active", "Closed" };
        if (!isAdmin && !((shop.Status == "Active" && dto.Status == "Closed") ||
                          (shop.Status == "Closed" && dto.Status == "Active")))
            return Forbid();
        if (!allowed.Contains(dto.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest("Invalid shop status");

        shop.Status = allowed.First(x => x.Equals(dto.Status, StringComparison.OrdinalIgnoreCase));
        shop.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Shop>().Update(shop);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating shop status");
    }

    [HttpGet("mine/dashboard"), Authorize]
    public async Task<ActionResult<MerchantDashboardReportDto>> GetMerchantDashboard()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var shop = (await unit.Repository<Shop>().ListAllAsync()).FirstOrDefault(x => x.OwnerUserId == userId);
        if (shop == null) return NotFound("Shop not found");

        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var shopOrders = (await unit.Repository<Order>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId)
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var nonCancelledOrders = shopOrders.Where(x => x.OrderStatus != "Cancelled").ToList();
        var orderIds = shopOrders.Select(x => x.OrderId).ToHashSet();

        var allOrderItems = (await unit.Repository<OrderItem>().ListAllAsync())
            .Where(x => orderIds.Contains(x.OrderId))
            .ToList();

        var shopProducts = (await unit.Repository<Product>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId && x.Status != "Archived")
            .ToList();

        var shopContents = (await unit.Repository<ContentEntity>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId && x.Status != "Archived")
            .ToList();

        var totalRevenue = nonCancelledOrders.Sum(x => x.TotalAmount);
        var todayRevenue = nonCancelledOrders.Where(x => x.CreatedAt >= todayStart).Sum(x => x.TotalAmount);
        var monthRevenue = nonCancelledOrders.Where(x => x.CreatedAt >= monthStart).Sum(x => x.TotalAmount);

        var pendingSlipOrders = shopOrders.Count(x => x.PaymentStatus == "Pending" || (!string.IsNullOrEmpty(x.SlipImageUrl) && x.OrderStatus == "Pending"));
        var pendingShipmentOrders = shopOrders.Count(x => x.OrderStatus is "Paid" or "Processing");
        var completedOrders = shopOrders.Count(x => x.OrderStatus == "Completed");

        var lowStockProducts = shopProducts
            .Where(x => x.Status == "Active" && x.Quantity <= 5)
            .OrderBy(x => x.Quantity)
            .ToList();

        var orderItemMap = allOrderItems
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => new { TotalUnits = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) }
            );

        var topProducts = shopProducts
            .Select(p => new MerchantTopProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                StockQuantity = p.Quantity,
                TotalUnitsSold = orderItemMap.TryGetValue(p.ProductId, out var stats) ? stats.TotalUnits : 0,
                TotalRevenue = orderItemMap.TryGetValue(p.ProductId, out stats) ? stats.Revenue : 0m
            })
            .OrderByDescending(p => p.TotalUnitsSold)
            .ThenByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToList();

        var salesTrend = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var day = todayStart.AddDays(-29 + offset);
                var dayOrders = nonCancelledOrders.Where(x => x.CreatedAt.Date == day).ToList();
                return new MerchantSalesTrendDto
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    DailyRevenue = dayOrders.Sum(x => x.TotalAmount),
                    OrdersCount = dayOrders.Count
                };
            })
            .ToList();

        var recentOrders = shopOrders.Take(5).Select(o => new MerchantRecentOrderDto
        {
            OrderId = o.OrderId,
            OrderNumber = o.OrderNumber,
            ReceiverName = o.ReceiverName,
            TotalAmount = o.TotalAmount,
            OrderStatus = o.OrderStatus,
            PaymentStatus = o.PaymentStatus,
            SlipImageUrl = o.SlipImageUrl,
            SlipUploadedAt = o.SlipUploadedAt,
            CreatedAt = o.CreatedAt,
            ItemsCount = allOrderItems.Count(i => i.OrderId == o.OrderId)
        }).ToList();

        var deliveryOrdersCount = shopOrders.Count(x => x.ShippingMethod == "Delivery");
        var pickupOrdersCount = shopOrders.Count(x => x.ShippingMethod == "Pickup");
        var avgOrderValue = nonCancelledOrders.Count > 0 ? Math.Round(nonCancelledOrders.Average(x => x.TotalAmount), 2) : 0m;

        var shopReviews = (await unit.Repository<Review>().ListAllAsync())
            .Where(x => x.ShopId == shop.ShopId && x.Status == "Published")
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var avgRating = shopReviews.Count == 0 ? 0 : Math.Round(shopReviews.Average(r => r.Rating), 1);
        var recentReviewDtos = shopReviews.Take(3).Select(r => new MerchantReviewItemDto
        {
            ReviewId = r.ReviewId,
            UserName = "ลูกค้าสั่งซื้อ",
            Rating = r.Rating,
            Comment = r.Comment,
            Reply = r.Reply,
            CreatedAt = r.CreatedAt
        }).ToList();

        return Ok(new MerchantDashboardReportDto
        {
            Summary = new MerchantSummaryDto
            {
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                MonthRevenue = monthRevenue,
                TotalOrders = shopOrders.Count,
                PendingSlipOrdersCount = pendingSlipOrders,
                PendingShipmentOrdersCount = pendingShipmentOrders,
                CompletedOrdersCount = completedOrders,
                TotalProducts = shopProducts.Count,
                LowStockProductsCount = lowStockProducts.Count,
                TotalContents = shopContents.Count
            },
            RecentOrders = recentOrders,
            TopProducts = topProducts,
            SalesTrend = salesTrend,
            LowStockProducts = lowStockProducts.Select(p => new MerchantLowStockProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                Quantity = p.Quantity
            }).ToList(),
            Fulfillment = new MerchantFulfillmentDto
            {
                DeliveryOrdersCount = deliveryOrdersCount,
                PickupOrdersCount = pickupOrdersCount,
                AverageOrderValue = avgOrderValue
            },
            ReviewsSummary = new MerchantReviewsSummaryDto
            {
                AverageRating = avgRating,
                TotalReviews = shopReviews.Count,
                RecentReviews = recentReviewDtos
            }
        });
    }

    private static void CopyDtoToShop(CreateShopDto dto, Shop shop)
    {
        shop.ShopName = dto.ShopName;
        shop.ShopCategoryId = dto.ShopCategoryId;
        shop.DistrictId = dto.DistrictId;
        shop.SubDistrictId = dto.SubDistrictId;
        shop.Description = dto.Description;
        shop.Phone = dto.Phone;
        shop.Email = dto.Email;
        shop.Address = dto.Address;
        shop.OpeningTime = dto.OpeningTime;
        shop.ClosingTime = dto.ClosingTime;
        shop.Latitude = dto.Latitude;
        shop.Longitude = dto.Longitude;
        shop.BankName = dto.BankName;
        shop.BankAccountName = dto.BankAccountName;
        shop.BankAccountNumber = dto.BankAccountNumber;
        shop.PromptPay = dto.PromptPay;
    }

    private static ShopDto ToDto(Shop shop, bool includeOwner = false) => new()
    {
        ShopId = shop.ShopId,
        OwnerUserId = includeOwner ? shop.OwnerUserId : null,
        ShopCategoryId = shop.ShopCategoryId,
        DistrictId = shop.DistrictId,
        SubDistrictId = shop.SubDistrictId,
        ShopName = shop.ShopName,
        Description = shop.Description,
        Phone = shop.Phone,
        Email = shop.Email,
        Address = shop.Address,
        CoverImageUrl = shop.CoverImageUrl,
        BackgroundImageUrl = shop.BackgroundImageUrl,
        OpeningTime = shop.OpeningTime,
        ClosingTime = shop.ClosingTime,
        Latitude = shop.Latitude,
        Longitude = shop.Longitude,
        Status = shop.Status,
        BankName = shop.BankName,
        BankAccountName = shop.BankAccountName,
        BankAccountNumber = shop.BankAccountNumber,
        PromptPay = shop.PromptPay,
        CreatedAt = shop.CreatedAt,
        UpdatedAt = shop.UpdatedAt,
        CategoryName = shop.ShopCategory?.CategoryName,
        DistrictName = shop.District?.DistrictName,
        SubDistrictName = shop.SubDistrict?.SubDistrictName
    };

    private async Task<string?> SaveCoverImageAsync(IFormFile file)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (file.Length is <= 0 or > 5 * 1024 * 1024 || !allowedTypes.Contains(file.ContentType.ToLowerInvariant())) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return null;

        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "shops");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/shops/{fileName}";
    }
}
