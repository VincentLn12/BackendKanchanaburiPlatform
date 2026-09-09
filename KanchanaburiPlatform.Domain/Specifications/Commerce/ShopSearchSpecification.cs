//using KanchanaburiPlatform.Domain.Entities;

//namespace KanchanaburiPlatform.Domain.Specifications.Commerce;

//public sealed class ShopSearchSpecification : BaseSpecification<Shop>
//{
//    public ShopSearchSpecification(
//        string? searchTerm = null,
//        Guid? categoryId = null,
//        Guid? districtId = null,
//        string? status = "Active",
//        int? page = null,
//        int pageSize = 20)
//        : base(shop =>
//            (string.IsNullOrWhiteSpace(searchTerm) || shop.ShopName.Contains(searchTerm)) &&
//            (!categoryId.HasValue || shop.ShopCategoryId == categoryId) &&
//            (!districtId.HasValue || shop.DistrictId == districtId) &&
//            (status == null || shop.Status == status))
//    {
//        AddInclude(shop => shop.ShopCategory);
//        AddInclude(shop => shop.District);
//        AddInclude(shop => shop.SubDistrict);
//        ApplyOrderBy(shop => shop.ShopName);

//        if (page.HasValue)
//        {
//            if (page.Value < 1) throw new ArgumentOutOfRangeException(nameof(page));
//            ApplyPaging((page.Value - 1) * pageSize, pageSize);
//        }
//    }
//}
