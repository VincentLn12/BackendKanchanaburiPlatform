using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public sealed class AddCartItemDto
{
    public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public sealed class UpdateCartItemDto
{
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public sealed class CartDto
{
    public Guid CartId { get; set; }
    public List<CartItemDto> Items { get; set; } = [];
    public decimal Total { get; set; }
}

public sealed class CartItemDto
{
    public Guid CartItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public string? ShopName { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal => Quantity * UnitPrice;
}
