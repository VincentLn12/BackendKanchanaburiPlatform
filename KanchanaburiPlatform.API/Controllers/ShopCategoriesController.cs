using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.DTOs;

namespace API.Controllers;

[ApiController]
[Route("api/shop-categories")]
public sealed class ShopCategoriesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ShopCategoryDto>>> GetCategories(int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var categories = await unit.Repository<ShopCategory>().ListAllAsync();
        var totalCount = categories.Count;
        return Ok(new PagedResultDto<ShopCategoryDto>
        {
            Items = categories.OrderBy(category => category.CategoryName).Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList(),
            Page = page, PageSize = pageSize, TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ShopCategoryDto>> GetCategory(Guid id) =>
        await unit.Repository<ShopCategory>().GetByIdAsync(id) is { } category ? Ok(ToDto(category)) : NotFound();

    [HttpGet("{id:guid}/image")]
    public async Task<IActionResult> GetImage(Guid id)
    {
        var category = await unit.Repository<ShopCategory>().GetByIdAsync(id);
        return category?.ImageData is { Length: > 0 } image ? File(image, category.ImageContentType ?? "application/octet-stream") : NotFound();
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ShopCategoryDto>> CreateCategory(CreateShopCategoryDto dto)
    {
        var category = new ShopCategory { ShopCategoryId = Guid.NewGuid(), CategoryName = dto.CategoryName, Description = dto.Description, Status = "Active" };
        unit.Repository<ShopCategory>().Add(category);
        return await unit.Complete() ? CreatedAtAction(nameof(GetCategory), new { id = category.ShopCategoryId }, ToDto(category)) : BadRequest();
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateShopCategoryDto dto)
    {
        var existing = await unit.Repository<ShopCategory>().GetByIdAsync(id);
        if (existing == null) return NotFound();
        existing.CategoryName = dto.CategoryName; existing.Description = dto.Description; existing.Status = dto.Status;
        // SaveChanges returns 0 when the admin submits an unchanged category.
        // That is still a valid idempotent update, especially when the next request uploads an image.
        await unit.Complete();
        return NoContent();
    }

    [HttpPost("{id:guid}/image"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile image)
    {
        var category = await unit.Repository<ShopCategory>().GetByIdAsync(id);
        if (category is null) return NotFound();
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (image.Length == 0 || image.Length > 2 * 1024 * 1024 || !allowedTypes.Contains(image.ContentType))
            return BadRequest("Use a JPG, PNG, or WebP image no larger than 2 MB.");
        await using var stream = new MemoryStream();
        await image.CopyToAsync(stream);
        category.ImageData = stream.ToArray();
        category.ImageContentType = image.ContentType;
        unit.Repository<ShopCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest("Problem saving image.");
    }

    [HttpDelete("{id:guid}/image"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteImage(Guid id)
    {
        var category = await unit.Repository<ShopCategory>().GetByIdAsync(id);
        if (category is null) return NotFound();
        category.ImageData = null;
        category.ImageContentType = null;
        unit.Repository<ShopCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest("Problem deleting image.");
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await unit.Repository<ShopCategory>().GetByIdAsync(id);
        if (category == null) return NotFound();
        category.Status = "Inactive"; unit.Repository<ShopCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    private static ShopCategoryDto ToDto(ShopCategory category) => new() { ShopCategoryId = category.ShopCategoryId, CategoryName = category.CategoryName, Description = category.Description, Status = category.Status, HasImage = category.ImageData is { Length: > 0 } };
}
