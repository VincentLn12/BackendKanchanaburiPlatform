using KanchanaburiPlatform.Domain.Entities;

namespace KanchanaburiPlatform.Domain.Specifications.Content;

public sealed class ContentSearchSpecification : BaseSpecification<Entities.Content>
{
    public ContentSearchSpecification(
        Guid? categoryId = null,
        Guid? districtId = null,
        Guid? subDistrictId = null,
        string? searchTerm = null,
        string? status = "Published",
        int? page = null,
        int pageSize = 20)
        : base(content =>
            (!categoryId.HasValue || content.ContentCategoryId == categoryId) &&
            (!districtId.HasValue || content.DistrictId == districtId) &&
            (!subDistrictId.HasValue || content.SubDistrictId == subDistrictId) &&
            (string.IsNullOrWhiteSpace(searchTerm) || content.Title.Contains(searchTerm)) &&
            (status == null || content.Status == status))
    {
        AddInclude(content => content.ContentCategory);
        AddInclude(content => content.Shop!);
        AddInclude(content => content.District!);
        AddInclude(content => content.SubDistrict!);
        AddInclude("ContentTags.Tag");
        ApplyOrderByDescending(content => content.PublishedAt ?? content.CreatedAt);

        if (page.HasValue)
        {
            if (page.Value < 1) throw new ArgumentOutOfRangeException(nameof(page));
            ApplyPaging((page.Value - 1) * pageSize, pageSize);
        }
    }
}
