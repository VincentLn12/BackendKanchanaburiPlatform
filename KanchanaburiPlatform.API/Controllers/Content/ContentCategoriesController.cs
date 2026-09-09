namespace API.Controllers.Content;

[ApiController]
[Route("api/content-categories")]
public sealed class ContentCategoriesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ContentCategoryDto>>> GetCategories(int page = 1, int pageSize = 100)
    {
        var categories = (await unit.Repository<ContentCategory>().ListAllAsync())
            .Where(category => category.Status == "Active")
            .OrderBy(category => category.CategoryName)
            .ToList();
        return Ok(ToPagedResult(categories, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContentCategoryDto>> GetCategory(Guid id)
    {
        var category = await unit.Repository<ContentCategory>().GetByIdAsync(id);
        return category is null || category.Status != "Active" ? NotFound() : Ok(ToDto(category));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ContentCategoryDto>>> GetCategoriesForAdmin(
        string? status, int page = 1, int pageSize = 20)
    {
        var categories = (await unit.Repository<ContentCategory>().ListAllAsync())
            .Where(category => string.IsNullOrWhiteSpace(status) || category.Status == status)
            .OrderBy(category => category.CategoryName)
            .ToList();
        return Ok(ToPagedResult(categories, page, pageSize));
    }

    [HttpGet("admin/{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentCategoryDto>> GetCategoryForAdmin(Guid id) =>
        await unit.Repository<ContentCategory>().GetByIdAsync(id) is { } category ? Ok(ToDto(category)) : NotFound();

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ContentCategoryDto>> CreateCategory(CreateContentCategoryDto dto)
    {
        var name = dto.CategoryName.Trim();
        if (await CategoryNameExists(name)) return Conflict("A content category with this name already exists.");

        var category = new ContentCategory
        {
            ContentCategoryId = Guid.NewGuid(),
            CategoryName = name,
            Description = dto.Description?.Trim(),
            Status = "Active"
        };
        unit.Repository<ContentCategory>().Add(category);
        return await unit.Complete()
            ? CreatedAtAction(nameof(GetCategoryForAdmin), new { id = category.ContentCategoryId }, ToDto(category))
            : BadRequest("Problem creating content category.");
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateContentCategoryDto dto)
    {
        var category = await unit.Repository<ContentCategory>().GetByIdAsync(id);
        if (category is null) return NotFound();
        var name = dto.CategoryName.Trim();
        if (await CategoryNameExists(name, id)) return Conflict("A content category with this name already exists.");
        if (dto.Status is not ("Active" or "Inactive")) return BadRequest("Invalid content category status.");

        category.CategoryName = name;
        category.Description = dto.Description?.Trim();
        category.Status = dto.Status;
        unit.Repository<ContentCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating content category.");
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await unit.Repository<ContentCategory>().GetByIdAsync(id);
        if (category is null) return NotFound();
        category.Status = "Inactive";
        unit.Repository<ContentCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest("Problem deleting content category.");
    }

    private async Task<bool> CategoryNameExists(string name, Guid? exceptId = null) =>
        (await unit.Repository<ContentCategory>().ListAllAsync()).Any(category =>
            category.ContentCategoryId != exceptId &&
            category.CategoryName.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static PagedResultDto<ContentCategoryDto> ToPagedResult(IReadOnlyList<ContentCategory> categories, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return new PagedResultDto<ContentCategoryDto>
        {
            Items = categories.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = categories.Count,
            TotalPages = (int)Math.Ceiling(categories.Count / (double)pageSize)
        };
    }

    private static ContentCategoryDto ToDto(ContentCategory category) => new()
    {
        ContentCategoryId = category.ContentCategoryId,
        CategoryName = category.CategoryName,
        Description = category.Description,
        Status = category.Status
    };
}
