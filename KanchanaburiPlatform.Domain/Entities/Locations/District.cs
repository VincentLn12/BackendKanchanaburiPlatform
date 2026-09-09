namespace KanchanaburiPlatform.Domain.Entities;

public sealed class District
{
    public Guid DistrictId { get; set; }

    public string DistrictName { get; set; } = string.Empty;
    public ICollection<SubDistrict> SubDistricts { get; set; } = new List<SubDistrict>();
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
    public ICollection<Content> Contents { get; set; } = new List<Content>();
}
