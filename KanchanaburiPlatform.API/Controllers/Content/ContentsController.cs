using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;
using KanchanaburiPlatform.Application.Specifications.Content;

namespace API.Controllers.Content;

[ApiController]
[Route("api/contents")]
public sealed class ContentsController(IUnitOfWork unit, StoreContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetContents(
        Guid? categoryId, Guid? districtId, Guid? subDistrictId, Guid? tagId, Guid? shopId, string? search, string? sortBy, int page = 1, int pageSize = 20)
    {
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(categoryId, districtId, subDistrictId, tagId, search, page: currentPage, pageSize: size, shopId: shopId, sortBy: sortBy)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentDto>> GetContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(contentId: id));
        if (content is null) return NotFound();
        var dto = ToDto(content);
        dto.ViewCount = await context.ContentViews.AsNoTracking().CountAsync(v => v.ContentId == id);
        return Ok(dto);
    }

    [HttpGet("{id:guid}/shop-products")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetShopProducts(Guid id, int take = 4)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(contentId: id));
        if (content is null) return NotFound();
        if (!content.ShopId.HasValue) return Ok(Array.Empty<ProductDto>());

        take = Math.Clamp(take, 1, 6);
        var products = (await unit.Repository<Product>().ListAllAsync())
            .Where(product => product.ShopId == content.ShopId.Value && product.Status == "Active")
            .OrderByDescending(product => product.UpdatedAt)
            .Take(take)
            .Select(product => new ProductDto
            {
                ProductId = product.ProductId,
                ShopId = product.ShopId,
                ShopName = content.Shop?.ShopName,
                ProductCategoryId = product.ProductCategoryId,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                ImageUrl = product.ImageUrl,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt
            }).ToList();
        return Ok(products);
    }

    [HttpGet("mine"), Authorize]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetMyContents(int page = 1, int pageSize = 20)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(status: null, createdByUserId: userId, page: currentPage, pageSize: size)));
    }

    [HttpGet("mine/{id:guid}"), Authorize]
    public async Task<ActionResult<ContentDto>> GetMyContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: id));
        return content is null ? NotFound() : CanManage(content) ? Ok(ToDto(content)) : Forbid();
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ContentDto>>> GetContentsForAdmin(
        string? status, int page = 1, int pageSize = 20)
    {
        return Ok(await GetPagedContents(page, pageSize, (currentPage, size) =>
            new ContentSearchSpecification(status: status, page: currentPage, pageSize: size)));
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentDto>> GetContentForAdmin(Guid id) =>
        await unit.Repository<ContentEntity>().GetEntityWithSpec(
            new ContentSearchSpecification(status: null, contentId: id)) is { } content ? Ok(ToDto(content)) : NotFound();

    [HttpPost, Authorize]
    public async Task<ActionResult<ContentDto>> CreateContent(CreateContentDto dto)
    {
        var error = await ValidateReferences(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanUseShop(dto.ShopId)) return Forbid();

        var initialStatus = string.IsNullOrWhiteSpace(dto.Status) ? "Published" : dto.Status;
        if (initialStatus is not ("Draft" or "Pending" or "Published")) initialStatus = "Published";

        var content = new ContentEntity
        {
            ContentId = Guid.NewGuid(),
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
            Status = initialStatus,
            PublishedAt = initialStatus == "Published" ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        CopyDto(dto, content);
        unit.Repository<ContentEntity>().Add(content);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetMyContent), new { id = content.ContentId }, ToDto(content))
            : BadRequest("Problem creating content.");
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateContent(Guid id, UpdateContentDto dto)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(id);
        if (content is null) return NotFound();
        if (!CanManage(content)) return Forbid();
        var error = await ValidateReferences(dto);
        if (error is not null) return BadRequest(error);
        if (!await CanUseShop(dto.ShopId)) return Forbid();
        var isAdmin = User.IsInRole("Admin");
        if (!isAdmin && content.Status == "Archived")
            return BadRequest("Archived content cannot be edited.");
        if (dto.Status is not ("Draft" or "Pending" or "Published" or "Archived")) return BadRequest("Invalid content status.");

        CopyDto(dto, content);
        content.Status = dto.Status;
        content.PublishedAt = dto.Status == "Published" ? content.PublishedAt ?? DateTime.UtcNow : null;
        content.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ContentEntity>().Update(content);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating content.");
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteContent(Guid id)
    {
        var content = await unit.Repository<ContentEntity>().GetByIdAsync(id);
        if (content is null) return NotFound();
        if (!CanManage(content)) return Forbid();
        content.Status = "Archived";
        content.UpdatedAt = DateTime.UtcNow;
        unit.Repository<ContentEntity>().Update(content);
        return await unit.Complete() ? NoContent() : BadRequest("Problem archiving content.");
    }

    private async Task<string?> ValidateReferences(SaveContentDto dto)
    {
        var category = await unit.Repository<ContentCategory>().GetByIdAsync(dto.ContentCategoryId);
        if (category is null || category.Status != "Active") return "Content category is not available.";
        if (dto.DistrictId is { } districtId && await unit.Repository<District>().GetByIdAsync(districtId) is null)
            return "District not found.";
        if (dto.SubDistrictId is { } subDistrictId)
        {
            var subDistrict = await unit.Repository<SubDistrict>().GetByIdAsync(subDistrictId);
            if (subDistrict is null || (dto.DistrictId.HasValue && subDistrict.DistrictId != dto.DistrictId.Value))
                return "Sub-district does not belong to the selected district.";
        }
        return null;
    }

    private async Task<bool> CanUseShop(Guid? shopId)
    {
        if (!shopId.HasValue) return true;
        var shop = await unit.Repository<Shop>().GetByIdAsync(shopId.Value);
        return shop is not null && shop.Status == "Active" &&
            (User.IsInRole("Admin") || shop.OwnerUserId == User.FindFirstValue(ClaimTypes.NameIdentifier));
    }

    private bool CanManage(ContentEntity content) => User.IsInRole("Admin") ||
        content.CreatedByUserId == User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<PagedResultDto<ContentDto>> GetPagedContents(
        int page, int pageSize, Func<int?, int, ContentSearchSpecification> createSpecification)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await unit.Repository<ContentEntity>().CountAsync(createSpecification(null, pageSize));
        var contents = await unit.Repository<ContentEntity>().ListAsync(createSpecification(page, pageSize));

        var dtos = contents.Select(ToDto).ToList();
        var contentIds = dtos.Select(x => x.ContentId).ToList();
        if (contentIds.Count > 0)
        {
            var viewCounts = await context.ContentViews
                .AsNoTracking()
                .Where(v => contentIds.Contains(v.ContentId))
                .GroupBy(v => v.ContentId)
                .Select(g => new { ContentId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ContentId, x => x.Count);

            foreach (var dto in dtos)
            {
                if (viewCounts.TryGetValue(dto.ContentId, out var count))
                {
                    dto.ViewCount = count;
                }
            }
        }

        return new PagedResultDto<ContentDto>
        {
            Items = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static void CopyDto(SaveContentDto dto, ContentEntity content)
    {
        content.Title = dto.Title.Trim();
        content.Summary = dto.Summary?.Trim();
        content.ContentCategoryId = dto.ContentCategoryId;
        content.ShopId = dto.ShopId;
        content.DistrictId = dto.DistrictId;
        content.SubDistrictId = dto.SubDistrictId;
        content.Latitude = dto.Latitude;
        content.Longitude = dto.Longitude;
        content.YoutubeUrl = dto.YoutubeUrl?.Trim();
    }
    private static ContentDto ToDto(ContentEntity content) => new()
    {
        ContentId = content.ContentId,
        CreatedByUserId = content.CreatedByUserId,
        ShopId = content.ShopId,
        ShopName = content.Shop?.ShopName,
        ContentCategoryId = content.ContentCategoryId,
        ContentCategoryName = content.ContentCategory?.CategoryName,
        DistrictId = content.DistrictId,
        DistrictName = content.District?.DistrictName,
        SubDistrictId = content.SubDistrictId,
        SubDistrictName = content.SubDistrict?.SubDistrictName,
        Title = content.Title,
        Summary = content.Summary,
        Latitude = content.Latitude,
        Longitude = content.Longitude,
        YoutubeUrl = content.YoutubeUrl,
        Status = content.Status,
        PublishedAt = content.PublishedAt,
        CreatedAt = content.CreatedAt,
        UpdatedAt = content.UpdatedAt,
        ViewCount = content.Views?.Count ?? 0,
        Tags = content.ContentTags.Select(contentTag => new TagDto
        {
            TagId = contentTag.TagId,
            TagName = contentTag.Tag.TagName,
            Status = contentTag.Tag.Status
        }).ToList()
    };

}
