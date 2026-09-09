using Application.DTOs;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/user-addresses")]
public sealed class UserAddressesController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<UserAddressDto>> GetMine()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        return (await unit.Repository<UserAddress>().ListAllAsync()).Where(x => x.UserId == userId).OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.UpdatedAt).Select(ToDto).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserAddressDto>> Create(SaveUserAddressDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var existing = (await unit.Repository<UserAddress>().ListAllAsync()).Where(x => x.UserId == userId).ToList();
        var address = new UserAddress { UserAddressId = Guid.NewGuid(), UserId = userId };
        Copy(dto, address);
        if (!existing.Any()) address.IsDefault = true;
        if (address.IsDefault) foreach (var item in existing) item.IsDefault = false;
        unit.Repository<UserAddress>().Add(address);
        return await unit.Complete() ? CreatedAtAction(nameof(GetMine), ToDto(address)) : BadRequest();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveUserAddressDto dto)
    {
        var address = await Mine(id); if (address == null) return NotFound();
        Copy(dto, address);
        if (address.IsDefault) await ClearDefault(address.UserId, id);
        unit.Repository<UserAddress>().Update(address);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpPatch("{id:guid}/default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        var address = await Mine(id); if (address == null) return NotFound();
        await ClearDefault(address.UserId, id); address.IsDefault = true; address.UpdatedAt = DateTime.UtcNow;
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var address = await Mine(id); if (address == null) return NotFound();
        unit.Repository<UserAddress>().Remove(address);
        return await unit.Complete() ? NoContent() : BadRequest();
    }

    private async Task<UserAddress?> Mine(Guid id) => (await unit.Repository<UserAddress>().ListAllAsync()).FirstOrDefault(x => x.UserAddressId == id && x.UserId == (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty));
    private async Task ClearDefault(string userId, Guid exceptId) { foreach (var item in (await unit.Repository<UserAddress>().ListAllAsync()).Where(x => x.UserId == userId && x.UserAddressId != exceptId && x.IsDefault)) item.IsDefault = false; }
    private static void Copy(SaveUserAddressDto dto, UserAddress address) { address.RecipientName = dto.RecipientName; address.RecipientPhone = dto.RecipientPhone; address.AddressLine = dto.AddressLine; address.SubDistrict = dto.SubDistrict; address.District = dto.District; address.Province = dto.Province; address.PostalCode = dto.PostalCode; address.IsDefault = dto.IsDefault; address.UpdatedAt = DateTime.UtcNow; }
    private static UserAddressDto ToDto(UserAddress x) => new() { UserAddressId = x.UserAddressId, RecipientName = x.RecipientName, RecipientPhone = x.RecipientPhone, AddressLine = x.AddressLine, SubDistrict = x.SubDistrict, District = x.District, Province = x.Province, PostalCode = x.PostalCode, IsDefault = x.IsDefault };
}
