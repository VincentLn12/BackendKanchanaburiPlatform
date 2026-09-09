namespace KanchanaburiPlatform.Domain.Entities;

public sealed class Content
{
    public Guid ContentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public Guid? ShopId { get; set; }

    public Guid ContentCategoryId { get; set; }

    public Guid? DistrictId { get; set; }

    public Guid? SubDistrictId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? YoutubeUrl { get; set; }

    public string Status { get; set; } = "Draft";
    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Shop? Shop { get; set; }

    public ContentCategory ContentCategory { get; set; } = null!;
    public District? District { get; set; }

    public SubDistrict? SubDistrict { get; set; }

    public ICollection<ContentTag> ContentTags { get; set; } = new List<ContentTag>();
    public ICollection<ContentRelation> RelatedContents { get; set; } = new List<ContentRelation>();
    public ICollection<ContentRelation> RelatedToContents { get; set; } = new List<ContentRelation>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
    public ICollection<ContentView> Views { get; set; } = new List<ContentView>();
    public ICollection<ContentFavorite> Favorites { get; set; } = new List<ContentFavorite>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
