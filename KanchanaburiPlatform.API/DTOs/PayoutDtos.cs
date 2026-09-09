namespace API.DTOs;

public sealed class AdminConfirmPayoutFormDto
{
    public Guid ShopId { get; set; }
    public string OrderIds { get; set; } = string.Empty;
    public string? TransactionRef { get; set; }
    public string? Note { get; set; }
    public IFormFile? SlipFile { get; set; }
}
