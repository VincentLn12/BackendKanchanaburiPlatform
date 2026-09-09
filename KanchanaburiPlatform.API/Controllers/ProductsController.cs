using System.Text.Json;

namespace API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IUnitOfWork unit, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ProductDto>> GetProducts() =>
        (await unit.Repository<Product>().ListAllAsync())
            .Where(x => x.Status == "Active")
            .Select(product => ToDto(product))
            .ToList();

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product?.Status != "Active") return NotFound();
        return Ok(ToDto(product));
    }

    [HttpGet("admin"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<PagedResultDto<ProductDto>>> GetProductsForAdmin(Guid? shopId, string? status, int page = 1, int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var products = await unit.Repository<Product>().ListAllAsync();
        var shops = await unit.Repository<Shop>().ListAllAsync();
        var shopNames = shops.ToDictionary(shop => shop.ShopId, shop => shop.ShopName);
        var filtered = products.Where(product => !shopId.HasValue || product.ShopId == shopId.Value)
            .Where(product => string.IsNullOrWhiteSpace(status) || product.Status == status)
            .OrderByDescending(product => product.UpdatedAt).ToList();
        return Ok(new PagedResultDto<ProductDto>
        {
            Items = filtered.Skip((page - 1) * pageSize).Take(pageSize).Select(product => ToDto(product, shopNames.GetValueOrDefault(product.ShopId))).ToList(),
            Page = page, PageSize = pageSize, TotalCount = filtered.Count,
            TotalPages = (int)Math.Ceiling(filtered.Count / (double)pageSize)
        });
    }

    [HttpPatch("{id:guid}/status"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductStatusForAdmin(Guid id, UpdateProductStatusDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();
        if (dto.Status is not ("Active" or "Inactive")) return BadRequest("Invalid product status");
        product.Status = dto.Status; product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Update(product);
        return await unit.Complete() ? NoContent() : BadRequest("Problem updating product status");
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        var shop = await unit.Repository<Shop>().GetByIdAsync(dto.ShopId);
        if (shop == null) return BadRequest("Shop not found");
        if (!CanManage(shop)) return Forbid();
        if (await unit.Repository<ProductCategory>().GetByIdAsync(dto.ProductCategoryId) == null) return BadRequest("Product category not found");
        var product = new Product { ProductId = Guid.NewGuid(), ShopId = dto.ShopId, ProductCategoryId = dto.ProductCategoryId, ProductName = dto.ProductName, Description = dto.Description, Price = dto.Price, Quantity = dto.Quantity, ImageUrl = dto.ImageUrl, Status = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        unit.Repository<Product>().Add(product);
        return await unit.Complete() ? CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, ToDto(product)) : BadRequest();
    }

    [HttpPut("{id:guid}"), Authorize]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();
        var shop = await unit.Repository<Shop>().GetByIdAsync(product.ShopId);
        if (shop == null || !CanManage(shop)) return Forbid();
        if (await unit.Repository<ProductCategory>().GetByIdAsync(dto.ProductCategoryId) == null) return BadRequest("Product category not found");
        product.ProductCategoryId = dto.ProductCategoryId; product.ProductName = dto.ProductName; product.Description = dto.Description; product.Price = dto.Price; product.Quantity = dto.Quantity; product.ImageUrl = dto.ImageUrl; product.Status = dto.Status; product.UpdatedAt = DateTime.UtcNow;
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpDelete("{id:guid}"), Authorize]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(id);
        if (product == null) return NotFound();
        var shop = await unit.Repository<Shop>().GetByIdAsync(product.ShopId);
        if (shop == null || !CanManage(shop)) return Forbid();
        product.Status = "Inactive"; product.UpdatedAt = DateTime.UtcNow; unit.Repository<Product>().Update(product);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpPost("{id:guid}/cover-image"), Authorize]
    public async Task<ActionResult<ProductDto>> UploadCoverImage(Guid id, IFormFile file)
    {
        var product = await GetManagedProduct(id);
        if (product == null) return NotFound();
        if (!CanManage(await unit.Repository<Shop>().GetByIdAsync(product.ShopId))) return Forbid();

        var imageUrl = await SaveImageAsync(file);
        if (imageUrl == null) return BadRequest("Only JPG, PNG, WEBP files up to 5 MB are allowed.");
        product.ImageUrl = imageUrl;
        product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Update(product);
        return await unit.Complete() ? Ok(ToDto(product)) : BadRequest("Problem uploading cover image");
    }

    [HttpPost("{id:guid}/detail-images"), Authorize]
    public async Task<ActionResult<IReadOnlyList<string>>> UploadDetailImages(Guid id, List<IFormFile> files)
    {
        var product = await GetManagedProduct(id);
        if (product == null) return NotFound();
        if (!CanManage(await unit.Repository<Shop>().GetByIdAsync(product.ShopId))) return Forbid();
        if (files.Count is 0 or > 8) return BadRequest("Upload 1 to 8 images at a time.");

        var images = GetDetailImages(product);
        foreach (var file in files)
        {
            var imageUrl = await SaveImageAsync(file);
            if (imageUrl == null) return BadRequest("Only JPG, PNG, WEBP files up to 5 MB are allowed.");
            images.Add(imageUrl);
        }
        product.DetailImageUrls = JsonSerializer.Serialize(images);
        product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Update(product);
        return await unit.Complete() ? Ok(images) : BadRequest("Problem uploading detail images");
    }

    [HttpDelete("{productId:guid}/detail-images/{imageIndex:int}"), Authorize]
    public async Task<IActionResult> DeleteDetailImage(Guid productId, int imageIndex)
    {
        var product = await GetManagedProduct(productId);
        if (product == null) return NotFound();
        if (!CanManage(await unit.Repository<Shop>().GetByIdAsync(product.ShopId))) return Forbid();
        var images = GetDetailImages(product);
        if (imageIndex < 0 || imageIndex >= images.Count) return NotFound();
        images.RemoveAt(imageIndex);
        product.DetailImageUrls = JsonSerializer.Serialize(images);
        product.UpdatedAt = DateTime.UtcNow;
        unit.Repository<Product>().Update(product);
        return await unit.Complete() ? NoContent() : BadRequest("Problem deleting detail image");
    }

    private bool CanManage(Shop? shop) => shop != null &&
        (User.IsInRole("Admin") ||
         (shop.Status == "Active" && shop.OwnerUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)));

    private async Task<Product?> GetManagedProduct(Guid id) => await unit.Repository<Product>().GetByIdAsync(id);

    private async Task<string?> SaveImageAsync(IFormFile file)
    {
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (file.Length is <= 0 or > 5 * 1024 * 1024 || !allowedTypes.Contains(file.ContentType.ToLowerInvariant())) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png" and not ".webp") return null;
        var folder = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "products");
        Directory.CreateDirectory(folder);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
        await file.CopyToAsync(stream);
        return $"/uploads/products/{fileName}";
    }

    private static List<string> GetDetailImages(Product product)
    {
        try { return JsonSerializer.Deserialize<List<string>>(product.DetailImageUrls) ?? []; }
        catch (JsonException) { return []; }
    }

    private static ProductDto ToDto(Product product, string? shopName = null) => new()
    {
        ProductId = product.ProductId,
        ShopId = product.ShopId,
        ShopName = shopName,
        ProductCategoryId = product.ProductCategoryId,
        ProductName = product.ProductName,
        Description = product.Description,
        Price = product.Price,
        Quantity = product.Quantity,
        ImageUrl = product.ImageUrl,
        Status = product.Status,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt,
        DetailImages = GetDetailImages(product)
    };
}
