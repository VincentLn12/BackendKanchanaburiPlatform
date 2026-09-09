using System.Linq.Expressions;
using Core.Interfaces;
using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Application.Specifications.Commerce;

// Specification เดียวสำหรับ list, detail และร้านของผู้ใช้
public sealed class ShopSearchSpecification : ISpecification<Shop>
{
    public ShopSearchSpecification(
        string? search = null,
        Guid? categoryId = null,
        Guid? districtId = null,
        Guid? subDistrictId = null,
        string? status = null,
        Guid? shopId = null,
        string? ownerUserId = null,
        string? sortBy = null,
        int? page = null,
        int pageSize = 20)
    {
        Criteria = shop =>
            (!shopId.HasValue || shop.ShopId == shopId.Value) &&
            (string.IsNullOrWhiteSpace(search) || shop.ShopName.Contains(search)) &&
            (!categoryId.HasValue || shop.ShopCategoryId == categoryId.Value) &&
            (!districtId.HasValue || shop.DistrictId == districtId.Value) &&
            (!subDistrictId.HasValue || shop.SubDistrictId == subDistrictId.Value) &&
            (status == null || shop.Status == status) &&
            (ownerUserId == null || shop.OwnerUserId == ownerUserId);

        Includes = [shop => shop.ShopCategory, shop => shop.District, shop => shop.SubDistrict];
        IncludeStrings = [];

        if (string.Equals(sortBy, "title", StringComparison.OrdinalIgnoreCase))
        {
            OrderBy = shop => shop.ShopName;
        }
        else
        {
            OrderByDescending = shop => shop.CreatedAt;
        }

        if (page.HasValue)
        {
            Skip = (page.Value - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
        }
    }

    public Expression<Func<Shop, bool>>? Criteria { get; }
    public Expression<Func<Shop, object>>? OrderBy { get; }
    public Expression<Func<Shop, object>>? OrderByDescending { get; }
    public List<Expression<Func<Shop, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take { get; }
    public int Skip { get; }
    public bool IsPagingEnabled { get; }

    public IQueryable<Shop> ApplyCriteria(IQueryable<Shop> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
