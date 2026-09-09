using System.Linq.Expressions;
using Core.Interfaces;
using ContentEntity = KanchanaburiPlatform.Domain.Entities.Content;

namespace KanchanaburiPlatform.Application.Specifications.Content;

public sealed class ContentSearchSpecification : ISpecification<ContentEntity>
{
    public ContentSearchSpecification(
        Guid? categoryId = null,
        Guid? districtId = null,
        Guid? subDistrictId = null,
        Guid? tagId = null,
        string? searchTerm = null,
        string? status = "Published",
        Guid? contentId = null,
        string? createdByUserId = null,
        int? page = null,
        int pageSize = 20,
        Guid? shopId = null,
        string? sortBy = null)
    {
        Criteria = content =>
            (!contentId.HasValue || content.ContentId == contentId.Value) &&
            (!categoryId.HasValue || content.ContentCategoryId == categoryId.Value) &&
            (!districtId.HasValue || content.DistrictId == districtId.Value) &&
            (!subDistrictId.HasValue || content.SubDistrictId == subDistrictId.Value) &&
            (!tagId.HasValue || content.ContentTags.Any(contentTag => contentTag.TagId == tagId.Value)) &&
            (!shopId.HasValue || content.ShopId == shopId.Value) &&
            (string.IsNullOrWhiteSpace(createdByUserId) || content.CreatedByUserId == createdByUserId) &&
            (string.IsNullOrWhiteSpace(searchTerm) || content.Title.ToLower().Contains(searchTerm.ToLower())) &&
            (status == null || content.Status == status);

        Includes = [content => content.ContentCategory, content => content.Shop!, content => content.District!, content => content.SubDistrict!];
        IncludeStrings = ["ContentTags.Tag"];

        if (sortBy == "title")
        {
            OrderBy = content => content.Title;
        }
        else if (sortBy == "popular")
        {
            OrderByDescending = content => content.Views.Count;
        }
        else
        {
            OrderByDescending = content => content.PublishedAt ?? content.CreatedAt;
        }

        if (page.HasValue)
        {
            if (page.Value < 1) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(pageSize));
            Skip = (page.Value - 1) * pageSize;
            Take = pageSize;
            IsPagingEnabled = true;
        }
    }

    public Expression<Func<ContentEntity, bool>>? Criteria { get; }
    public Expression<Func<ContentEntity, object>>? OrderBy { get; }
    public Expression<Func<ContentEntity, object>>? OrderByDescending { get; }
    public List<Expression<Func<ContentEntity, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public bool IsDistinct => false;
    public int Take { get; }
    public int Skip { get; }
    public bool IsPagingEnabled { get; }
    public IQueryable<ContentEntity> ApplyCriteria(IQueryable<ContentEntity> query) =>
        Criteria is null ? query : query.Where(Criteria);
}
