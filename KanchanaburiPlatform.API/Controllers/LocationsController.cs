namespace API.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController(IUnitOfWork unit) : ControllerBase
{
    [HttpGet("districts")]
    public Task<IReadOnlyList<District>> GetDistricts() => unit.Repository<District>().ListAllAsync();

    [HttpPost("districts")]
    public async Task<ActionResult<District>> CreateDistrict(District district)
    {
        district.DistrictId = Guid.NewGuid(); unit.Repository<District>().Add(district);
        return await unit.Complete() ? Ok(district) : BadRequest();
    }

    [HttpGet("districts/{districtId:guid}/sub-districts")]
    public async Task<IReadOnlyList<SubDistrict>> GetSubDistricts(Guid districtId) =>
        (await unit.Repository<SubDistrict>().ListAllAsync()).Where(x => x.DistrictId == districtId).ToList();

    [HttpPost("sub-districts")]
    public async Task<ActionResult<SubDistrict>> CreateSubDistrict(SubDistrict subDistrict)
    {
        if (await unit.Repository<District>().GetByIdAsync(subDistrict.DistrictId) == null) return BadRequest("District not found");
        subDistrict.SubDistrictId = Guid.NewGuid(); unit.Repository<SubDistrict>().Add(subDistrict);
        return await unit.Complete() ? Ok(subDistrict) : BadRequest();
    }
}
