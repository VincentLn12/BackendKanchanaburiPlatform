namespace Application.DTOs;

public sealed class CheckoutDto
{
    public Guid? ShopId { get; set; }
    [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue)]
    public decimal ShippingFee { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string ShippingMethod { get; set; } = "Delivery";
    public string? ReceiverName { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ShippingAddress { get; set; }
}
