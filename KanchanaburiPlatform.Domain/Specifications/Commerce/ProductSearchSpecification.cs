using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Domain.Specifications.Commerce;

public sealed class ProductSearchSpecification : BaseSpecification<Product>
{
    public ProductSearchSpecification(
        Guid? shopId = null,
        Guid? categoryId = null,
        string? searchTerm = null,
        decimal? minimumPrice = null,
        decimal? maximumPrice = null,
        string? status = "Active",
        int? page = null,
        int pageSize = 20)
        : base(product =>
            (!shopId.HasValue || product.ShopId == shopId) &&
            (!categoryId.HasValue || product.ProductCategoryId == categoryId) &&
            (string.IsNullOrWhiteSpace(searchTerm) || product.ProductName.Contains(searchTerm)) &&
            (!minimumPrice.HasValue || product.Price >= minimumPrice) &&
            (!maximumPrice.HasValue || product.Price <= maximumPrice) &&
            (status == null || product.Status == status))
    {
        AddInclude(product => product.Shop);
        AddInclude(product => product.ProductCategory);
        ApplyOrderBy(product => product.ProductName);

        if (page.HasValue)
        {
            if (page.Value < 1) throw new ArgumentOutOfRangeException(nameof(page));
            ApplyPaging((page.Value - 1) * pageSize, pageSize);
        }
    }
}
