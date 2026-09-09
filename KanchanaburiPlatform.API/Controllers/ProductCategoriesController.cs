namespace API.Controllers;

[ApiController]
[Route("api/product-categories")]
public sealed class ProductCategoriesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductCategoryDto>>> GetCategories(int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var categories = await unit.Repository<ProductCategory>().ListAllAsync();
        var totalCount = categories.Count;
        return Ok(new PagedResultDto<ProductCategoryDto>
        {
            Items = categories.OrderBy(category => category.CategoryName).Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToList(),
            Page = page, PageSize = pageSize, TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductCategoryDto>> GetCategory(Guid id) =>
        await unit.Repository<ProductCategory>().GetByIdAsync(id) is { } category ? Ok(ToDto(category)) : NotFound();

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductCategoryDto>> CreateCategory(CreateProductCategoryDto dto)
    {
        var category = new ProductCategory
        {
            ProductCategoryId = Guid.NewGuid(),
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            Status = "Active"
        };
        unit.Repository<ProductCategory>().Add(category);
        return await unit.Complete() ? CreatedAtAction(nameof(GetCategory), new { id = category.ProductCategoryId }, ToDto(category)) : BadRequest();
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateProductCategoryDto dto)
    {
        var category = await unit.Repository<ProductCategory>().GetByIdAsync(id);
        if (category == null) return NotFound();
        category.CategoryName = dto.CategoryName; category.Description = dto.Description; category.Status = dto.Status;
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var category = await unit.Repository<ProductCategory>().GetByIdAsync(id);
        if (category == null) return NotFound();
        category.Status = "Inactive"; unit.Repository<ProductCategory>().Update(category);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    private static ProductCategoryDto ToDto(ProductCategory category) => new() { ProductCategoryId = category.ProductCategoryId, CategoryName = category.CategoryName, Description = category.Description, Status = category.Status };
}
