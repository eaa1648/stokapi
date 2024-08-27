using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class AdminUserRoleController : ControllerBase
{
    private readonly UserRoleService _userRoleService;

    public AdminUserRoleController(UserRoleService userRoleService)
    {
        _userRoleService = userRoleService;
    }

    [HttpGet("user-role")]
    //[Authorize(Roles = "admin")] // Sadece admin rolüne sahip kullanıcıların erişebileceği endpoint
    public async Task<IActionResult> GetUserRole([FromQuery] string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return BadRequest("Username is required.");
        }

        var role = await _userRoleService.GetRoleByUsernameAsync(username);
        return Ok(new { Username = username, Role = role });
    }
}
