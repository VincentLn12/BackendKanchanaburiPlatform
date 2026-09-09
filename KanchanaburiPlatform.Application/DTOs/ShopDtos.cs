using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateShopDto
{
    [Required, StringLength(200)] public string ShopName { get; set; } = string.Empty;
    public Guid ShopCategoryId { get; set; }
    public Guid DistrictId { get; set; }
    public Guid SubDistrictId { get; set; }
    public string? Description { get; set; }
    public string? Phone { get; set; }
    [EmailAddress] public string? Email { get; set; }
    public string? Address { get; set; }
    public string? CoverImageUrl { get; set; }
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")] public string? OpeningTime { get; set; }
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$")] public string? ClosingTime { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? PromptPay { get; set; }
}

public sealed class UpdateShopDto : CreateShopDto { }

public sealed class UpdateShopStatusDto
{
    [Required] public string Status { get; set; } = string.Empty;
}

public sealed class ShopDto
{
    public Guid ShopId { get; set; }
    public string? OwnerUserId { get; set; }
    public Guid ShopCategoryId { get; set; }
    public Guid DistrictId { get; set; }
    public Guid SubDistrictId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CategoryName { get; set; }
    public string? DistrictName { get; set; }
    public string? SubDistrictName { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? PromptPay { get; set; }
}

public sealed class ShopCategoryDto
{
    public Guid ShopCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool HasImage { get; set; }
}

public class CreateShopCategoryDto
{
    [Required, StringLength(150)] public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateShopCategoryDto : CreateShopCategoryDto
{
    [Required] public string Status { get; set; } = "Active";
}

public sealed class MerchantDashboardReportDto
{
    public MerchantSummaryDto Summary { get; set; } = new();
    public List<MerchantRecentOrderDto> RecentOrders { get; set; } = [];
    public List<MerchantTopProductDto> TopProducts { get; set; } = [];
    public List<MerchantSalesTrendDto> SalesTrend { get; set; } = [];
    public List<MerchantLowStockProductDto> LowStockProducts { get; set; } = [];
    public MerchantFulfillmentDto Fulfillment { get; set; } = new();
    public MerchantReviewsSummaryDto ReviewsSummary { get; set; } = new();
}

public sealed class MerchantSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int PendingSlipOrdersCount { get; set; }
    public int PendingShipmentOrdersCount { get; set; }
    public int CompletedOrdersCount { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProductsCount { get; set; }
    public int TotalContents { get; set; }
}

public sealed class MerchantRecentOrderDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ReceiverName { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? SlipImageUrl { get; set; }
    public DateTime? SlipUploadedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
}

public sealed class MerchantTopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public int TotalUnitsSold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public sealed class MerchantSalesTrendDto
{
    public string Date { get; set; } = string.Empty;
    public decimal DailyRevenue { get; set; }
    public int OrdersCount { get; set; }
}

public sealed class MerchantLowStockProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public sealed class MerchantFulfillmentDto
{
    public int DeliveryOrdersCount { get; set; }
    public int PickupOrdersCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public sealed class MerchantReviewsSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<MerchantReviewItemDto> RecentReviews { get; set; } = [];
}

public sealed class MerchantReviewItemDto
{
    public Guid ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Reply { get; set; }
    public DateTime CreatedAt { get; set; }
}
