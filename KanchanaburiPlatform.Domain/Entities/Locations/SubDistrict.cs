namespace KanchanaburiPlatform.Domain.Entities;

public sealed class SubDistrict
{
    public Guid SubDistrictId { get; set; }

    public Guid DistrictId { get; set; }

    public string SubDistrictName { get; set; } = string.Empty;
    public string? PostalCode { get; set; }

    public District District { get; set; } = null!;
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
