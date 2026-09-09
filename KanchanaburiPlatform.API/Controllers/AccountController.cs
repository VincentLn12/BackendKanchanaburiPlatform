using System.ComponentModel.DataAnnotations;

namespace API.Controllers;

[ApiController, Authorize]
[Route("api/account")]
public sealed class AccountController(UserManager<AppUser> userManager) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        return Ok(await ToDto(user));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null ? Unauthorized() : Ok(await ToProfileDto(user));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ProfileDto>> UpdateProfile(UpdateProfileDto dto)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(error => error.Description));
        return Ok(await ToProfileDto(user));
    }

    private async Task<CurrentUserDto> ToDto(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserDto { Id = user.Id, Name = $"{user.FirstName} {user.LastName}".Trim(), Email = user.Email ?? string.Empty, Role = roles.FirstOrDefault() ?? "User" };
    }

    private async Task<ProfileDto> ToProfileDto(AppUser user)
    {
        var currentUser = await ToDto(user);
        return new ProfileDto { Id = currentUser.Id, FirstName = user.FirstName ?? string.Empty, LastName = user.LastName ?? string.Empty, Name = currentUser.Name, Email = currentUser.Email, Role = currentUser.Role };
    }
}

public class CurrentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
}

public sealed class UpdateProfileDto
{
    [StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [StringLength(100)] public string LastName { get; set; } = string.Empty;
}

public sealed class ProfileDto : CurrentUserDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
