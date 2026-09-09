using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class SaveUserAddressDto
{
    [Required, StringLength(150)] public string RecipientName { get; set; } = string.Empty;
    [Required, StringLength(30)] public string RecipientPhone { get; set; } = string.Empty;
    [Required, StringLength(500)] public string AddressLine { get; set; } = string.Empty;
    public string? SubDistrict { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class UserAddressDto : SaveUserAddressDto
{
    public Guid UserAddressId { get; set; }
}
