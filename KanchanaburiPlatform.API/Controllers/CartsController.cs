namespace API.Controllers;

[ApiController, Authorize]
[Route("api/cart")]
public sealed class CartsController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var cart = (await unit.Repository<Cart>().ListAllAsync()).FirstOrDefault(x => x.UserId == UserId());
        return Ok(await ToDto(cart));
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem(AddCartItemDto dto)
    {
        var product = await unit.Repository<Product>().GetByIdAsync(dto.ProductId);
        if (product == null || product.Status != "Active") return BadRequest("สินค้านี้ไม่พร้อมจำหน่าย");
        if (product.Quantity <= 0) return BadRequest("สินค้าหมดแล้ว");

        var unitPrice = product.Price;

        var cart = await GetOrCreateCartAsync();
        var items = await unit.Repository<CartItem>().ListAllAsync();
        var item = items.FirstOrDefault(x => x.CartId == cart.CartId && x.ProductId == dto.ProductId);
        var requestedQuantity = (item?.Quantity ?? 0) + dto.Quantity;
        if (requestedQuantity > product.Quantity)
            return BadRequest($"สินค้านี้มีในสต็อก {product.Quantity} ชิ้น แต่ในตะกร้าของคุณมี {item?.Quantity ?? 0} ชิ้นแล้ว จึงไม่สามารถเพิ่มได้อีก");
        if (item == null)
        {
            item = new CartItem { CartItemId = Guid.NewGuid(), CartId = cart.CartId, ProductId = dto.ProductId, Quantity = dto.Quantity, UnitPrice = unitPrice };
            unit.Repository<CartItem>().Add(item);
        }
        else { item.Quantity += dto.Quantity; item.UnitPrice = unitPrice; unit.Repository<CartItem>().Update(item); }
        cart.UpdatedAt = DateTime.UtcNow; unit.Repository<Cart>().Update(cart);
        return await unit.Complete() ? Ok(await ToDto(cart)) : BadRequest();
    }

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, UpdateCartItemDto dto)
    {
        if (dto.Quantity <= 0) return BadRequest("จำนวนสินค้าต้องมากกว่า 0");
        var item = await unit.Repository<CartItem>().GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await IsMyCartItem(item)) return Forbid();
        var product = await unit.Repository<Product>().GetByIdAsync(item.ProductId);
        if (product == null || product.Status != "Active") return BadRequest("สินค้านี้ไม่พร้อมจำหน่าย");
        if (dto.Quantity > product.Quantity) return BadRequest($"สินค้านี้มีในสต็อก {product.Quantity} ชิ้น กรุณาเลือกจำนวนไม่เกิน {product.Quantity} ชิ้น");
        item.Quantity = dto.Quantity; unit.Repository<CartItem>().Update(item);
        var cart = await unit.Repository<Cart>().GetByIdAsync(item.CartId); if (cart != null) { cart.UpdatedAt = DateTime.UtcNow; unit.Repository<Cart>().Update(cart); }
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await unit.Repository<CartItem>().GetByIdAsync(id);
        if (item == null) return NotFound();
        if (!await IsMyCartItem(item)) return Forbid();
        unit.Repository<CartItem>().Remove(item);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private async Task<bool> IsMyCartItem(CartItem item)
    {
        var cart = await unit.Repository<Cart>().GetByIdAsync(item.CartId);
        return cart?.UserId == UserId();
    }

    private async Task<Cart> GetOrCreateCartAsync()
    {
        var cart = (await unit.Repository<Cart>().ListAllAsync()).FirstOrDefault(x => x.UserId == UserId());
        if (cart != null) return cart;
        cart = new Cart { CartId = Guid.NewGuid(), UserId = UserId(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        unit.Repository<Cart>().Add(cart); await unit.Complete(); return cart;
    }

    private async Task<CartDto> ToDto(Cart? cart)
    {
        if (cart == null) return new CartDto();
        var products = (await unit.Repository<Product>().ListAllAsync()).ToDictionary(x => x.ProductId);
        var shops = (await unit.Repository<Shop>().ListAllAsync()).ToDictionary(x => x.ShopId);
        var items = (await unit.Repository<CartItem>().ListAllAsync()).Where(x => x.CartId == cart.CartId).Select(item =>
        {
            products.TryGetValue(item.ProductId, out var product);
            Shop? shop = null;
            if (product != null) shops.TryGetValue(product.ShopId, out shop);
            return new CartItemDto { CartItemId = item.CartItemId, ProductId = item.ProductId, ShopId = product?.ShopId ?? Guid.Empty, ShopName = shop?.ShopName ?? "ร้านค้าชุมชน", ProductName = product?.ProductName ?? "สินค้าไม่พร้อมขาย", ImageUrl = product?.ImageUrl, Quantity = item.Quantity, AvailableQuantity = product?.Quantity ?? 0, UnitPrice = item.UnitPrice };
        }).ToList();
        return new CartDto { CartId = cart.CartId, Items = items, Total = items.Sum(x => x.Subtotal) };
    }
}
